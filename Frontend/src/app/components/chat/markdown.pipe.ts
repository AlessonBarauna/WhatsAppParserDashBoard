import { Pipe, PipeTransform, SecurityContext } from '@angular/core';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';

/**
 * Minimal Markdown→HTML renderer (bold, italic, code blocks, inline code,
 * tables, and unordered/ordered lists). No external dependency needed.
 */
@Pipe({ name: 'markdown', standalone: true, pure: true })
export class MarkdownPipe implements PipeTransform {
  constructor(private sanitizer: DomSanitizer) {}

  transform(value: string): SafeHtml {
    if (!value) return '';
    const html = this.parseMarkdown(value);
    return this.sanitizer.bypassSecurityTrustHtml(html);
  }

  private parseMarkdown(md: string): string {
    const lines = md.split('\n');
    const out: string[] = [];
    let inCodeBlock = false;
    let inTable = false;
    let tableHeader = false;
    let inList = false;

    for (let i = 0; i < lines.length; i++) {
      const line = lines[i];

      // Fenced code block
      if (line.startsWith('```')) {
        if (!inCodeBlock) {
          if (inList) { out.push('</ul>'); inList = false; }
          if (inTable) { out.push('</tbody></table>'); inTable = false; }
          inCodeBlock = true;
          out.push('<pre><code>');
        } else {
          inCodeBlock = false;
          out.push('</code></pre>');
        }
        continue;
      }

      if (inCodeBlock) {
        out.push(this.escapeHtml(line));
        continue;
      }

      // Table row
      if (line.trim().startsWith('|')) {
        const cells = line.split('|').slice(1, -1).map(c => c.trim());
        if (!inTable) {
          if (inList) { out.push('</ul>'); inList = false; }
          inTable = true;
          tableHeader = true;
          out.push('<table class="fx-table" style="margin:0.75rem 0;"><thead><tr>');
          cells.forEach(c => out.push(`<th>${this.inline(c)}</th>`));
          out.push('</tr></thead><tbody>');
        } else if (tableHeader && cells.every(c => /^[-:]+$/.test(c))) {
          tableHeader = false; // separator row — skip
        } else {
          out.push('<tr>');
          cells.forEach(c => out.push(`<td>${this.inline(c)}</td>`));
          out.push('</tr>');
        }
        continue;
      } else if (inTable) {
        out.push('</tbody></table>');
        inTable = false;
        tableHeader = false;
      }

      // Headings
      if (line.startsWith('### ')) { out.push(`<h6 style="margin:0.75rem 0 0.25rem">${this.inline(line.slice(4))}</h6>`); continue; }
      if (line.startsWith('## '))  { out.push(`<h5 style="margin:0.75rem 0 0.25rem">${this.inline(line.slice(3))}</h5>`); continue; }
      if (line.startsWith('# '))   { out.push(`<h4 style="margin:0.75rem 0 0.25rem">${this.inline(line.slice(2))}</h4>`); continue; }

      // Horizontal rule
      if (/^---+$/.test(line.trim())) { out.push('<hr style="border-color:var(--border-dim);margin:0.75rem 0">'); continue; }

      // Unordered list
      if (/^[-*] /.test(line)) {
        if (!inList) { out.push('<ul style="margin:0.5rem 0 0.5rem 1.25rem;padding:0">'); inList = true; }
        out.push(`<li>${this.inline(line.slice(2))}</li>`);
        continue;
      } else if (inList) {
        out.push('</ul>');
        inList = false;
      }

      // Empty line
      if (line.trim() === '') {
        out.push('<br>');
        continue;
      }

      out.push(`<p style="margin:0.15rem 0">${this.inline(line)}</p>`);
    }

    if (inList)  out.push('</ul>');
    if (inTable) out.push('</tbody></table>');
    if (inCodeBlock) out.push('</code></pre>');

    return out.join('\n');
  }

  private inline(text: string): string {
    return text
      .replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;')
      .replace(/`([^`]+)`/g, '<code style="background:rgba(0,240,255,.1);padding:0 0.3em;border-radius:3px">$1</code>')
      .replace(/\*\*([^*]+)\*\*/g, '<strong>$1</strong>')
      .replace(/\*([^*]+)\*/g, '<em>$1</em>');
  }

  private escapeHtml(text: string): string {
    return text.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;');
  }
}
