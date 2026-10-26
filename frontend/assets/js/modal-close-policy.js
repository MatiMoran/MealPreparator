const ModalClosePolicy = {
    bindExplicitDismissal(cancelButton, closeButton, closeModal) {
        cancelButton.addEventListener('click', closeModal);
        closeButton.addEventListener('click', closeModal);
    },

    shouldDismissOnEscape(recipeModal, ingredientModal, inlineIngredientModal) {
        return !recipeModal.classList.contains('active') &&
            !ingredientModal.classList.contains('active') &&
            !inlineIngredientModal.classList.contains('active');
    }
};

if (typeof module !== 'undefined' && module.exports) {
    module.exports = ModalClosePolicy;
}

if (typeof globalThis !== 'undefined') {
    globalThis.ModalClosePolicy = ModalClosePolicy;
}
