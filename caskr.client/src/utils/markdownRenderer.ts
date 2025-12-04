/**
 * Simple markdown renderer for FAQ and similar content.
 * Supports: bold, italic, links, bullet lists, inline code.
 * Does NOT support: images, headers, complex formatting.
 */

export interface MarkdownRenderOptions {
  /** Allow links to be rendered as <a> tags */
  allowLinks?: boolean;
  /** Class name to apply to rendered container */
  className?: string;
}

/**
 * Converts a subset of markdown to HTML string.
 * Use dangerouslySetInnerHTML to render the result.
 *
 * Supported syntax:
 * - **bold** or __bold__
 * - *italic* or _italic_
 * - [link text](url)
 * - `inline code`
 * - - bullet list items (at start of line)
 * - Line breaks (\n\n for paragraph, \n for <br>)
 */
export function renderMarkdown(text: string, options: MarkdownRenderOptions = {}): string {
  const { allowLinks = true } = options;

  let html = escapeHtml(text);

  // Convert bold: **text** or __text__
  html = html.replace(/\*\*(.+?)\*\*/g, '<strong>$1</strong>');
  html = html.replace(/__(.+?)__/g, '<strong>$1</strong>');

  // Convert italic: *text* or _text_ (but not inside words)
  html = html.replace(/(?<![*_])\*([^*]+?)\*(?![*_])/g, '<em>$1</em>');
  html = html.replace(/(?<![*_])_([^_]+?)_(?![*_])/g, '<em>$1</em>');

  // Convert inline code: `code`
  html = html.replace(/`([^`]+?)`/g, '<code>$1</code>');

  // Convert links: [text](url)
  if (allowLinks) {
    html = html.replace(
      /\[([^\]]+?)\]\(([^)]+?)\)/g,
      '<a href="$2" target="_blank" rel="noopener noreferrer">$1</a>'
    );
  } else {
    // Remove link syntax but keep text
    html = html.replace(/\[([^\]]+?)\]\([^)]+?\)/g, '$1');
  }

  // Convert bullet lists
  // Match lines starting with - or *
  const lines = html.split('\n');
  let inList = false;
  const processedLines: string[] = [];

  for (let i = 0; i < lines.length; i++) {
    const line = lines[i];
    const bulletMatch = line.match(/^[-*]\s+(.+)$/);

    if (bulletMatch) {
      if (!inList) {
        processedLines.push('<ul>');
        inList = true;
      }
      processedLines.push(`<li>${bulletMatch[1]}</li>`);
    } else {
      if (inList) {
        processedLines.push('</ul>');
        inList = false;
      }
      processedLines.push(line);
    }
  }

  if (inList) {
    processedLines.push('</ul>');
  }

  html = processedLines.join('\n');

  // Convert double line breaks to paragraphs
  // First, normalize line breaks
  html = html.replace(/\r\n/g, '\n');

  // Split by double newlines for paragraphs
  const paragraphs = html.split(/\n\n+/);

  if (paragraphs.length > 1) {
    html = paragraphs
      .map(p => p.trim())
      .filter(p => p.length > 0)
      .map(p => {
        // Don't wrap lists in <p>
        if (p.startsWith('<ul>') || p.startsWith('<li>')) {
          return p;
        }
        // Convert single newlines to <br> within paragraph
        const withBreaks = p.replace(/\n/g, '<br />');
        return `<p>${withBreaks}</p>`;
      })
      .join('');
  } else {
    // Single paragraph - convert single newlines to <br>
    html = html.replace(/\n/g, '<br />');
  }

  return html;
}

/**
 * Creates a safe object for dangerouslySetInnerHTML
 */
export function createMarkdownHtml(
  text: string,
  options?: MarkdownRenderOptions
): { __html: string } {
  return { __html: renderMarkdown(text, options) };
}

/**
 * Escapes HTML special characters to prevent XSS
 */
function escapeHtml(text: string): string {
  const escapeMap: Record<string, string> = {
    '&': '&amp;',
    '<': '&lt;',
    '>': '&gt;',
    '"': '&quot;',
    "'": '&#x27;',
  };

  return text.replace(/[&<>"']/g, char => escapeMap[char] || char);
}

/**
 * Strips all markdown formatting and returns plain text
 */
export function stripMarkdown(text: string): string {
  let plain = text;

  // Remove bold
  plain = plain.replace(/\*\*(.+?)\*\*/g, '$1');
  plain = plain.replace(/__(.+?)__/g, '$1');

  // Remove italic
  plain = plain.replace(/\*(.+?)\*/g, '$1');
  plain = plain.replace(/_(.+?)_/g, '$1');

  // Remove inline code
  plain = plain.replace(/`([^`]+?)`/g, '$1');

  // Remove links but keep text
  plain = plain.replace(/\[([^\]]+?)\]\([^)]+?\)/g, '$1');

  // Remove bullet markers
  plain = plain.replace(/^[-*]\s+/gm, '');

  return plain;
}

export default renderMarkdown;
