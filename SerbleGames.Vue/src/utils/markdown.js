import { marked } from 'marked';

// Configure marked for safe HTML rendering
marked.setOptions({
  breaks: true,
  gfm: true,
});

/**
 * Convert markdown text to HTML
 * @param {string} markdown - The markdown text to render
 * @returns {string} HTML string
 */
export function renderMarkdown(markdown) {
  if (!markdown) return '';
  try {
    return marked(markdown);
  } catch (e) {
    console.error('Error rendering markdown:', e);
    return markdown;
  }
}

/**
 * Convert markdown to plain text (strips markdown formatting)
 * @param {string} markdown - The markdown text
 * @returns {string} Plain text
 */
export function markdownToPlainText(markdown) {
  if (!markdown) return '';
  return markdown
    .replace(/^#{1,6}\s+/gm, '')
    .replace(/\*\*(.+?)\*\*/g, '$1')
    .replace(/\*(.+?)\*/g, '$1')
    .replace(/__(.+?)__/g, '$1')
    .replace(/_(.+?)_/g, '$1')
    .replace(/\[(.+?)\]\(.+?\)/g, '$1')
    .replace(/`(.+?)`/g, '$1')
    .replace(/```[\s\S]*?```/g, '')
    .replace(/\n+/g, ' ')
    .trim();
}
