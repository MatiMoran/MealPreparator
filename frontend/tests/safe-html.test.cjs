const assert = require('node:assert/strict');
const test = require('node:test');
const { escapeHtml } = require('../assets/js/safe-html.js');

test('escapeHtml encodes every HTML-significant character in user-provided text', () => {
    assert.equal(
        escapeHtml(`<img src=x onerror="alert('x')"> &`),
        '&lt;img src=x onerror=&quot;alert(&#39;x&#39;)&quot;&gt; &amp;'
    );
});

test('escapeHtml converts nullish values to empty text', () => {
    assert.equal(escapeHtml(null), '');
    assert.equal(escapeHtml(undefined), '');
});
