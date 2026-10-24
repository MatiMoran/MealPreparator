const API_BASE = 'http://localhost:5080';

let recipes = [];
let selections = {};

const recipesGrid = document.getElementById('recipes-grid');
const generateBtn = document.getElementById('generate-list');
const generateHint = document.getElementById('generate-hint');
const navGenerate = document.getElementById('nav-generate');
const navGenerateMobile = document.getElementById('nav-generate-mobile');
const navMenu = document.getElementById('nav-menu');
const navMobile = document.getElementById('nav-mobile');
const navLogo = document.getElementById('nav-logo');
const modal = document.getElementById('shopping-modal');
const modalBackdrop = document.getElementById('modal-backdrop');
const modalClose = document.getElementById('modal-close');
const modalBody = document.getElementById('modal-body');

navMenu.addEventListener('click', () => {
    navMobile.classList.toggle('active');
});

navLogo.addEventListener('click', (e) => {
    e.preventDefault();
    window.scrollTo({ top: 0, behavior: 'smooth' });
});

document.querySelectorAll('.nav__mobile-link').forEach(link => {
    link.addEventListener('click', () => {
        navMobile.classList.remove('active');
    });
});

const scrollToRecipes = (e) => {
    e.preventDefault();
    navMobile.classList.remove('active');
    document.getElementById('armar-lista').scrollIntoView({ behavior: 'smooth', block: 'start' });
};

navGenerate.addEventListener('click', scrollToRecipes);
navGenerateMobile.addEventListener('click', scrollToRecipes);
document.querySelector('.hero__cta').addEventListener('click', (e) => {
    e.preventDefault();
    document.getElementById('armar-lista').scrollIntoView({ behavior: 'smooth', block: 'start' });
});

async function loadRecipes() {
    try {
        const res = await fetch(`${API_BASE}/api/recipes`);
        if (!res.ok) throw new Error('Error al cargar recetas');
        recipes = await res.json();
        
        recipesGrid.innerHTML = '';
        
        for (const recipe of recipes) {
            selections[recipe.id] = 0;
            const card = document.createElement('div');
            card.className = 'recipe-card';
            card.innerHTML = `
                <div class="recipe-card__image-wrapper">
                    <img src="${recipe.imageUrl ? API_BASE + '/' + recipe.imageUrl : 'https://images.unsplash.com/photo-1546069901-ba9599a7e63c?auto=format&fit=crop&q=80&w=1400'}" alt="${recipe.name}" class="recipe-card__image" onerror="this.src='https://images.unsplash.com/photo-1546069901-ba9599a7e63c?auto=format&fit=crop&q=80&w=1400'">
                </div>
                <div class="recipe-card__content">
                    <div class="recipe-card__header">
                        <h3 class="recipe-card__name">${recipe.name}</h3>
                        <span class="recipe-card__servings">Rinde ${recipe.servings} porciones</span>
                    </div>
                    <div class="recipe-card__controls">
                        <span class="recipe-card__label">Porciones</span>
                        <div class="recipe-card__counter">
                            <button class="recipe-card__btn" data-action="dec" data-id="${recipe.id}">-</button>
                            <span class="recipe-card__count" id="count-${recipe.id}">0</span>
                            <button class="recipe-card__btn" data-action="inc" data-id="${recipe.id}">+</button>
                        </div>
                    </div>
                </div>
            `;
            recipesGrid.appendChild(card);
        }
        
        recipesGrid.querySelectorAll('.recipe-card__btn').forEach(btn => {
            btn.addEventListener('click', () => {
                const id = parseInt(btn.dataset.id);
                const action = btn.dataset.action;
                if (action === 'inc') {
                    selections[id] = Math.min((selections[id] || 0) + 1, 99);
                } else if (action === 'dec') {
                    selections[id] = Math.max((selections[id] || 0) - 1, 0);
                }
                document.getElementById(`count-${id}`).textContent = selections[id];
                updateGenerateButton();
            });
        });
    } catch (err) {
        recipesGrid.innerHTML = `<div class="recipes__loading">No se pudo conectar con la API. Asegurate de que esté corriendo en ${API_BASE}</div>`;
    }
}

function updateGenerateButton() {
    const hasSelection = Object.values(selections).some(v => v > 0);
    generateBtn.disabled = !hasSelection;
    generateHint.style.display = hasSelection ? 'none' : 'block';
}

generateBtn.addEventListener('click', async () => {
    const items = [];
    for (const [recipeId, portions] of Object.entries(selections)) {
        if (portions > 0) {
            items.push({ recipeId: parseInt(recipeId), portions: parseFloat(portions) });
        }
    }
    
    if (items.length === 0) return;
    
    generateBtn.disabled = true;
    generateBtn.textContent = 'Generando...';
    
    try {
        const res = await fetch(`${API_BASE}/api/shopping-list`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ items })
        });
        
        if (!res.ok) throw new Error('Error al generar lista');
        const data = await res.json();
        
        modalBody.innerHTML = '';
        if (data.length === 0) {
            modalBody.innerHTML = '<p style="color: var(--text-secondary);">Sin ingredientes.</p>';
        } else {
            for (const item of data) {
                const div = document.createElement('div');
                div.className = 'modal__item';
                div.innerHTML = `
                    <span class="modal__item-name">${item.ingredientName}</span>
                    <span class="modal__item-qty">${item.quantity} ${item.unitDisplay}</span>
                `;
                modalBody.appendChild(div);
            }
        }
        
        modal.classList.add('active');
        document.body.style.overflow = 'hidden';
    } catch (err) {
        alert('Error al generar la lista de compras. Verificá que la API esté corriendo.');
    } finally {
        generateBtn.disabled = false;
        generateBtn.textContent = 'Generar ingredientes';
        updateGenerateButton();
    }
});

const closeModal = () => {
    modal.classList.remove('active');
    document.body.style.overflow = '';
};

modalBackdrop.addEventListener('click', closeModal);
modalClose.addEventListener('click', closeModal);
document.addEventListener('keydown', (e) => {
    if (e.key === 'Escape') closeModal();
});

loadRecipes();
