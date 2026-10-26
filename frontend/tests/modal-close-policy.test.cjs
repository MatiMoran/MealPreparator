const assert = require('node:assert/strict');
const test = require('node:test');
const {
    bindExplicitDismissal,
    shouldDismissOnEscape
} = require('../assets/js/modal-close-policy.js');

test('Cancel and the X button close the modal, but the backdrop does not', () => {
    const cancelButton = new EventTarget();
    const backdrop = new EventTarget();
    const closeButton = new EventTarget();
    let closeCount = 0;
    bindExplicitDismissal(cancelButton, closeButton, () => closeCount++);

    backdrop.dispatchEvent(new Event('click'));
    assert.equal(closeCount, 0);

    closeButton.dispatchEvent(new Event('click'));
    assert.equal(closeCount, 1);

    cancelButton.dispatchEvent(new Event('click'));

    assert.equal(closeCount, 2);
});

test('Escape is ignored while a recipe, ingredient, or inline ingredient modal is open', () => {
    const activeModal = { classList: { contains: name => name === 'active' } };
    const inactiveModal = { classList: { contains: () => false } };

    assert.equal(shouldDismissOnEscape(activeModal, inactiveModal, inactiveModal), false);
    assert.equal(shouldDismissOnEscape(inactiveModal, activeModal, inactiveModal), false);
    assert.equal(shouldDismissOnEscape(inactiveModal, inactiveModal, activeModal), false);
    assert.equal(shouldDismissOnEscape(inactiveModal, inactiveModal, inactiveModal), true);
});
