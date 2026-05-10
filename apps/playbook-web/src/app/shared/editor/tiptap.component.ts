import {
  Component, ElementRef, ViewChild, Input, Output, EventEmitter,
  AfterViewInit, OnDestroy, OnChanges, SimpleChanges,
  ChangeDetectorRef, inject
} from '@angular/core';
import { Editor } from '@tiptap/core';
import StarterKit from '@tiptap/starter-kit';
import Link from '@tiptap/extension-link';

@Component({
  selector: 'app-tiptap',
  standalone: true,
  template: `
<div class="tiptap-editor">
  @if (editable) {
    <div class="flex items-center gap-1 px-3 py-2 border-b border-ink-200 dark:border-ink-800 bg-ink-50 dark:bg-ink-900/50 flex-wrap">
      <button type="button" (click)="cmd('toggleBold')"
        [class.text-indigo-600]="editor?.isActive('bold')"
        class="p-1.5 rounded hover:bg-ink-100 dark:hover:bg-ink-800 text-sm font-bold text-ink-600 dark:text-ink-300 transition">B</button>
      <button type="button" (click)="cmd('toggleItalic')"
        [class.text-indigo-600]="editor?.isActive('italic')"
        class="p-1.5 rounded hover:bg-ink-100 dark:hover:bg-ink-800 text-sm italic text-ink-600 dark:text-ink-300 transition">I</button>
      <button type="button" (click)="cmd('toggleStrike')"
        [class.text-indigo-600]="editor?.isActive('strike')"
        class="p-1.5 rounded hover:bg-ink-100 dark:hover:bg-ink-800 text-sm line-through text-ink-600 dark:text-ink-300 transition">S</button>
      <div class="w-px h-4 bg-ink-200 dark:bg-ink-700 mx-1"></div>
      <button type="button" (click)="cmd('toggleBulletList')"
        [class.text-indigo-600]="editor?.isActive('bulletList')"
        class="p-1.5 rounded hover:bg-ink-100 dark:hover:bg-ink-800 text-ink-600 dark:text-ink-300 transition text-xs">≡</button>
      <button type="button" (click)="cmd('toggleOrderedList')"
        [class.text-indigo-600]="editor?.isActive('orderedList')"
        class="p-1.5 rounded hover:bg-ink-100 dark:hover:bg-ink-800 text-ink-600 dark:text-ink-300 transition text-xs">1.</button>
      <div class="w-px h-4 bg-ink-200 dark:bg-ink-700 mx-1"></div>
      <button type="button" (click)="cmd('toggleBlockquote')"
        [class.text-indigo-600]="editor?.isActive('blockquote')"
        class="p-1.5 rounded hover:bg-ink-100 dark:hover:bg-ink-800 text-ink-600 dark:text-ink-300 transition text-xs">"</button>
      <button type="button" (click)="cmd('toggleCode')"
        [class.text-indigo-600]="editor?.isActive('code')"
        class="p-1.5 rounded hover:bg-ink-100 dark:hover:bg-ink-800 font-mono text-xs text-ink-600 dark:text-ink-300 transition">&#123;&#125;</button>
    </div>
  }
  <div #editorEl></div>
</div>
  `,
  styles: [`
    :host { display: block; }
    :host ::ng-deep .ProseMirror { outline: none; min-height: 120px; padding: 0.75rem 1rem; }
    :host ::ng-deep .ProseMirror h1 { font-size: 1.25rem; font-weight: 600; margin: 0.75rem 0 0.5rem; }
    :host ::ng-deep .ProseMirror h2 { font-size: 1.1rem;  font-weight: 600; margin: 0.75rem 0 0.5rem; }
    :host ::ng-deep .ProseMirror p  { margin: 0.25rem 0; }
    :host ::ng-deep .ProseMirror ul, :host ::ng-deep .ProseMirror ol { padding-left: 1.25rem; }
    :host ::ng-deep .ProseMirror blockquote { border-left: 3px solid #6366f1; padding-left: 0.75rem; color: #78716c; }
    :host ::ng-deep .ProseMirror code { background: #f5f5f4; border-radius: 3px; padding: 0 4px; font-family: 'JetBrains Mono', monospace; font-size: 0.875em; }
  `]
})
export class TiptapComponent implements AfterViewInit, OnDestroy, OnChanges {
  private readonly cdr = inject(ChangeDetectorRef);

  @ViewChild('editorEl') editorEl!: ElementRef<HTMLElement>;
  @Input() content: unknown = null;
  @Input() editable = true;
  @Input() placeholder = 'Start writing…';
  @Output() contentChange = new EventEmitter<unknown>();

  editor?: Editor;

  ngAfterViewInit(): void {
    this.editor = new Editor({
      element: this.editorEl.nativeElement,
      extensions: [
        StarterKit,
        Link.configure({ openOnClick: !this.editable, HTMLAttributes: { class: 'text-indigo-600 underline' } })
      ],
      content: this.content as any,
      editable: this.editable,
      onUpdate: ({ editor }) => this.contentChange.emit(editor.getJSON())
    });
    this.cdr.detectChanges();
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['content'] && !changes['content'].firstChange && this.editor) {
      const cur = JSON.stringify(this.editor.getJSON());
      const next = JSON.stringify(changes['content'].currentValue);
      if (cur !== next) {
        this.editor.commands.setContent(changes['content'].currentValue as any);
      }
    }
    if (changes['editable'] && !changes['editable'].firstChange && this.editor) {
      this.editor.setEditable(changes['editable'].currentValue);
    }
  }

  ngOnDestroy(): void {
    this.editor?.destroy();
  }

  cmd(command: string): void {
    (this.editor?.chain().focus() as any)[command]?.().run();
  }
}
