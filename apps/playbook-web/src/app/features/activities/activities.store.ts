import { Injectable, computed, inject, signal } from '@angular/core';
import { Apollo, gql } from 'apollo-angular';
import { toSignal } from '@angular/core/rxjs-interop';
import { map, startWith } from 'rxjs';

export interface Category { id: string; name: string; color: string; }
export interface Attachment { blobPath: string; fileName: string; contentType: string; sizeBytes: number; downloadUrl: string; }

export interface Activity {
  id: string;
  title: string;
  description: unknown;
  notes: unknown;
  category: Category | null;
  tags: string[];
  dos: string[];
  donts: string[];
  attachments: Attachment[];
  isFavorite: boolean;
  isArchived: boolean;
  isStale: boolean;
  viewCount: number;
  lastViewedAt: string | null;
  createdAt: string;
  updatedAt: string;
}

export interface ActivityFilter {
  categoryId?: string;
  tags?: string[];
  search?: string;
  favoritesOnly?: boolean;
  includeArchived?: boolean;
}

const ACTIVITY_FIELDS = `
  id title isFavorite isArchived isStale viewCount updatedAt createdAt lastViewedAt
  tags dos donts
  category { id name color }
  attachments { blobPath fileName contentType sizeBytes downloadUrl }
  description notes
`;

export const LIST_ACTIVITIES = gql`
  query ListActivities($filter: ActivityFilterInput, $sort: ActivitySort, $first: Int, $after: String) {
    activities(filter: $filter, sort: $sort, first: $first, after: $after) {
      nodes { ${ACTIVITY_FIELDS} }
      pageInfo { hasNextPage endCursor }
    }
  }
`;

export const GET_ACTIVITY = gql`
  query GetActivity($id: String!) {
    activity(id: $id) { ${ACTIVITY_FIELDS} }
  }
`;

export const RECENTLY_VIEWED = gql`
  query RecentlyViewed { recentlyViewed(limit: 6) { ${ACTIVITY_FIELDS} } }
`;

export const FAVORITES = gql`
  query Favorites { favorites(limit: 6) { ${ACTIVITY_FIELDS} } }
`;

export const CREATE_ACTIVITY = gql`
  mutation CreateActivity($input: CreateActivityInput!) {
    createActivity(input: $input) { ${ACTIVITY_FIELDS} }
  }
`;

export const UPDATE_ACTIVITY = gql`
  mutation UpdateActivity($id: String!, $input: UpdateActivityInput!) {
    updateActivity(id: $id, input: $input) { ${ACTIVITY_FIELDS} }
  }
`;

export const DELETE_ACTIVITY = gql`
  mutation DeleteActivity($id: String!) { deleteActivity(id: $id) }
`;

export const TOGGLE_FAVORITE = gql`
  mutation ToggleFavorite($id: String!) { toggleFavorite(id: $id) { id isFavorite } }
`;

export const ARCHIVE_ACTIVITY = gql`
  mutation ArchiveActivity($id: String!, $archived: Boolean!) { archiveActivity(id: $id, archived: $archived) { id isArchived } }
`;

export const RECORD_VIEW = gql`
  mutation RecordView($id: String!) { recordView(id: $id) { id viewCount lastViewedAt isStale } }
`;

export const STALE_ACTIVITIES = gql`
  query StaleActivities($limit: Int) { staleActivities(limit: $limit) { ${ACTIVITY_FIELDS} } }
`;

export const ATTACH_FILE = gql`
  mutation AttachFile($activityId: String!, $blobPath: String!, $fileName: String!, $contentType: String!, $sizeBytes: Long!) {
    attachToActivity(activityId: $activityId, blobPath: $blobPath, fileName: $fileName, contentType: $contentType, sizeBytes: $sizeBytes) { id attachments { blobPath fileName contentType sizeBytes downloadUrl } }
  }
`;

export const DETACH_FILE = gql`
  mutation DetachFile($activityId: String!, $blobPath: String!) {
    detachFromActivity(activityId: $activityId, blobPath: $blobPath) { id attachments { blobPath fileName contentType sizeBytes downloadUrl } }
  }
`;

@Injectable({ providedIn: 'root' })
export class ActivitiesStore {
  private readonly apollo = inject(Apollo);

  createActivity(input: Record<string, unknown>) {
    return this.apollo.mutate<{ createActivity: Activity }>({ mutation: CREATE_ACTIVITY, variables: { input } });
  }

  updateActivity(id: string, input: Record<string, unknown>) {
    return this.apollo.mutate<{ updateActivity: Activity }>({ mutation: UPDATE_ACTIVITY, variables: { id, input } });
  }

  deleteActivity(id: string) {
    return this.apollo.mutate<{ deleteActivity: boolean }>({
      mutation: DELETE_ACTIVITY, variables: { id },
      refetchQueries: [{ query: LIST_ACTIVITIES }]
    });
  }

  toggleFavorite(id: string) {
    return this.apollo.mutate<{ toggleFavorite: Pick<Activity, 'id' | 'isFavorite'> }>({
      mutation: TOGGLE_FAVORITE, variables: { id }
    });
  }

  archiveActivity(id: string, archived: boolean) {
    return this.apollo.mutate<{ archiveActivity: Pick<Activity, 'id' | 'isArchived'> }>({
      mutation: ARCHIVE_ACTIVITY, variables: { id, archived }
    });
  }

  recordView(id: string) {
    return this.apollo.mutate({ mutation: RECORD_VIEW, variables: { id } });
  }

  attachFile(activityId: string, blobPath: string, fileName: string, contentType: string, sizeBytes: number) {
    return this.apollo.mutate({ mutation: ATTACH_FILE, variables: { activityId, blobPath, fileName, contentType, sizeBytes } });
  }

  detachFile(activityId: string, blobPath: string) {
    return this.apollo.mutate({ mutation: DETACH_FILE, variables: { activityId, blobPath } });
  }
}
