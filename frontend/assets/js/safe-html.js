const SafeHtml = {
    escapeHtml(value) {
        return String(value ?? '').replace(/[&<>"']/g, character => ({
            '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;'
        })[character]);
    }
};

if (typeof module !== 'undefined' && module.exports) {
    module.exports = SafeHtml;
}

if (typeof globalThis !== 'undefined') {
    globalThis.SafeHtml = SafeHtml;
}
