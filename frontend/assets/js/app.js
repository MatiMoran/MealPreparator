const API_BASE = 'http://localhost:5080';

let recipes = [];
let ingredients = [];
let selections = {};

const recipesGrid = document.getElementById('recipes-grid');
const generateBtn = document.getElementById('generate-list');
const generateHint = document.getElementById('generate-hint');
const navGenerate = document.getElementById('nav-generate');
const navGenerateMobile = document.getElementById('nav-generate-mobile');
const navMenu = document.getElementById('nav-menu');
const navMobile = document.getElementById('nav-mobile');
const navLogo = document.getElementById('nav-logo');
const heroCta = document.getElementById('hero-cta');

const modal = document.getElementById('shopping-modal');
const modalBackdrop = document.getElementById('modal-backdrop');
const modalClose = document.getElementById('modal-close');
const modalBody = document.getElementById('modal-body');
const modalCopy = document.getElementById('modal-copy');
const modalCopyFeedback = document.getElementById('modal-copy-feedback');

const viewHome = document.getElementById('view-home');
const viewGestor = document.getElementById('view-gestor');

const toastEl = document.getElementById('toast');
function showToast(msg) {
    toastEl.textContent = msg;
    toastEl.classList.add('active');
    setTimeout(() => toastEl.classList.remove('active'), 2000);
}

// routing
function applyRoute() {
    const hash = location.hash || '#home';
    if (hash.startsWith('#gestor')) {
        viewHome.style.display = 'none';
        viewGestor.style.display = 'block';
        window.scrollTo({ top: 0, behavior: 'auto' });
        loadGestor();
    } else {
        viewHome.style.display = 'block';
        viewGestor.style.display = 'none';
        if (hash === '#home') {
            // stay
        }
    }
}
window.addEventListener('hashchange', applyRoute);

// nav
navMenu.addEventListener('click', () => navMobile.classList.toggle('active'));
navLogo.addEventListener('click', (e) => { e.preventDefault(); location.hash = '#home'; window.scrollTo({ top: 0, behavior: 'smooth' }); });

document.querySelectorAll('.nav__mobile-link, .nav__link-home, .footer__link-home').forEach(a => {
    a.addEventListener('click', () => { navMobile.classList.remove('active'); });
});
document.querySelectorAll('.nav__link-about, .nav__mobile-link-about, .footer__link-about').forEach(a => {
    a.addEventListener('click', (e) => {
        e.preventDefault();
        navMobile.classList.remove('active');
        if (viewHome.style.display === 'none') { location.hash = '#home'; setTimeout(() => document.getElementById('about').scrollIntoView({ behavior: 'smooth' }), 50); }
        else document.getElementById('about').scrollIntoView({ behavior: 'smooth' });
    });
});

const scrollToRecipes = (e) => {
    e.preventDefault();
    navMobile.classList.remove('active');
    if (viewHome.style.display === 'none') { location.hash = '#home'; setTimeout(() => document.getElementById('armar-lista').scrollIntoView({ behavior: 'smooth', block: 'start' }), 50); }
    else document.getElementById('armar-lista').scrollIntoView({ behavior: 'smooth', block: 'start' });
};
navGenerate.addEventListener('click', scrollToRecipes);
navGenerateMobile.addEventListener('click', scrollToRecipes);
heroCta.addEventListener('click', scrollToRecipes);

// load recipes
async function loadRecipes() {
    try {
        const res = await fetch(`${API_BASE}/api/recipes`);
        if (!res.ok) throw new Error('Error');
        recipes = await res.json();
        recipesGrid.innerHTML = '';
        for (const recipe of recipes) {
            selections[recipe.id] = selections[recipe.id] || 0;
            const card = document.createElement('div');
            card.className = 'recipe-card';
            card.innerHTML = `
                <div class="recipe-card__image-wrapper">
                    <img src="${recipe.imageUrl ? (recipe.imageUrl.startsWith('http') ? recipe.imageUrl : API_BASE + '/' + recipe.imageUrl) : 'https://images.unsplash.com/photo-1546069901-ba9599a7e63c?auto=format&fit=crop&q=80&w=1400'}" alt="${recipe.name}" class="recipe-card__image" onerror="this.src='https://images.unsplash.com/photo-1546069901-ba9599a7e63c?auto=format&fit=crop&q=80&w=1400'">
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
                            <span class="recipe-card__count" id="count-${recipe.id}">${selections[recipe.id]}</span>
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
                if (btn.dataset.action === 'inc') selections[id] = Math.min((selections[id]||0)+1, 99);
                else selections[id] = Math.max((selections[id]||0)-1, 0);
                document.getElementById(`count-${id}`).textContent = selections[id];
                updateGenerateButton();
            });
        });
    } catch (e) {
        recipesGrid.innerHTML = `<div class="recipes__loading">No se pudo conectar con la API en ${API_BASE}</div>`;
    }
}

function updateGenerateButton() {
    const has = Object.values(selections).some(v => v > 0);
    generateBtn.disabled = !has;
    generateHint.style.display = has ? 'none' : 'block';
}

let lastShoppingList = [];
generateBtn.addEventListener('click', async () => {
    const items = [];
    for (const [rid, p] of Object.entries(selections)) if (p > 0) items.push({ recipeId: parseInt(rid), portions: parseFloat(p) });
    if (items.length === 0) return;
    generateBtn.disabled = true; generateBtn.textContent = 'Generando...';
    try {
        const res = await fetch(`${API_BASE}/api/shopping-list`, { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ items }) });
        if (!res.ok) throw new Error();
        const data = await res.json();
        lastShoppingList = data;
        modalBody.innerHTML = '';
        if (data.length === 0) modalBody.innerHTML = '<p style="color:var(--text-secondary);">Sin ingredientes.</p>';
        else {
            for (const item of data) {
                const div = document.createElement('div'); div.className = 'modal__item';
                div.innerHTML = `<span class="modal__item-name">${item.ingredientName}</span><span class="modal__item-qty">${formatIngredientAmount(item)}</span>`;
                modalBody.appendChild(div);
            }
        }
        modal.classList.add('active'); document.body.style.overflow = 'hidden';
        modalCopyFeedback.textContent = '';
    } catch(e) {
        alert('Error al generar la lista de compras');
    } finally {
        generateBtn.disabled = false; generateBtn.textContent = 'Generar ingredientes'; updateGenerateButton();
    }
});

modalCopy.addEventListener('click', async () => {
    if (!lastShoppingList || lastShoppingList.length === 0) return;
    const max = Math.max(...lastShoppingList.map(i => (i.ingredientName||'').length), 0);
    const text = lastShoppingList.map(i => `${(i.ingredientName||'').padEnd(max+2)}${formatIngredientAmount(i)}`).join('\n');
    try {
        await navigator.clipboard.writeText(text);
        modalCopyFeedback.textContent = '¡Copiado!';
        setTimeout(() => modalCopyFeedback.textContent = '', 2000);
    } catch(e) {
        modalBody.insertAdjacentHTML('beforeend', `<textarea readonly style="width:100%;margin-top:1rem;">${text}</textarea>`);
        modalCopyFeedback.textContent = 'Copiá manualmente';
    }
});

const closeModal = () => { modal.classList.remove('active'); document.body.style.overflow = ''; };
modalBackdrop.addEventListener('click', closeModal);
modalClose.addEventListener('click', closeModal);
document.addEventListener('keydown', (e)=>{ if(e.key==='Escape'){ closeModal(); closeRecipeModal(); closeIngredientModal(); closeInlineModal(); }});

// gestor
const gestorRecipes = document.getElementById('gestor-recipes-list');
const gestorIngredients = document.getElementById('gestor-ingredients-list');
document.getElementById('btn-new-recipe').addEventListener('click', () => openRecipeModal());
document.getElementById('btn-new-ingredient').addEventListener('click', () => openIngredientModal());

async function loadGestor() {
    try {
        const [rres, ires] = await Promise.all([fetch(`${API_BASE}/api/recipes`), fetch(`${API_BASE}/api/ingredients`)]);
        recipes = await rres.json(); ingredients = await ires.json();
        renderGestorRecipes(); renderGestorIngredients(); populateRecipeIngSelects();
    } catch(e) { showToast('Error cargando gestor'); }
}

function renderGestorRecipes() {
    gestorRecipes.innerHTML = '';
    for (const r of recipes) {
        const div = document.createElement('div'); div.className = 'gestor__item';
        div.innerHTML = `<img class="gestor__item-img" src="${r.imageUrl?(r.imageUrl.startsWith('http')?r.imageUrl:API_BASE+'/'+r.imageUrl):'https://images.unsplash.com/photo-1546069901-ba9599a7e63c?auto=format&fit=crop&q=80&w=400'}" onerror="this.src='https://images.unsplash.com/photo-1546069901-ba9599a7e63c?auto=format&fit=crop&q=80&w=400'"><div class="gestor__item-info"><span class="gestor__item-name">${r.name}</span><span class="gestor__item-meta">Porciones: ${r.servings}</span></div><div class="gestor__item-actions"><button class="gestor__action" data-act="edit" data-id="${r.id}">Editar</button><button class="gestor__action gestor__action--danger" data-act="del" data-id="${r.id}">Eliminar</button></div>`;
        gestorRecipes.appendChild(div);
    }
    gestorRecipes.querySelectorAll('button').forEach(b => b.addEventListener('click', async ()=>{
        const id = parseInt(b.dataset.id);
        if (b.dataset.act === 'edit') {
            const det = await fetch(`${API_BASE}/api/recipes/${id}`).then(x=>x.json());
            openRecipeModal(det);
        } else {
            if (!confirm('¿Eliminar receta? No borra ingredientes.')) return;
            const res = await fetch(`${API_BASE}/api/recipes/${id}`, { method: 'DELETE' });
            if (res.ok) { showToast('Receta eliminada'); loadGestor(); loadRecipes(); }
            else showToast('Error al eliminar');
        }
    }));
}

function renderGestorIngredients() {
    gestorIngredients.innerHTML = '';
    for (const ing of ingredients) {
        const div = document.createElement('div'); div.className = 'gestor__item';
        div.innerHTML = `<img class="gestor__item-img" src="${ing.imageUrl?(ing.imageUrl.startsWith('http')?ing.imageUrl:API_BASE+'/'+ing.imageUrl):'https://images.unsplash.com/photo-1546069901-ba9599a7e63c?auto=format&fit=crop&q=80&w=400'}" onerror="this.src='https://images.unsplash.com/photo-1546069901-ba9599a7e63c?auto=format&fit=crop&q=80&w=400'"><div class="gestor__item-info"><span class="gestor__item-name">${ing.name}</span><span class="gestor__item-meta">${ing.unitDisplay || ing.unit}</span></div><div class="gestor__item-actions"><button class="gestor__action" data-act="edit" data-id="${ing.id}">Editar</button><button class="gestor__action gestor__action--danger" data-act="del" data-id="${ing.id}">Eliminar</button></div>`;
        gestorIngredients.appendChild(div);
    }
    gestorIngredients.querySelectorAll('button').forEach(b => b.addEventListener('click', async ()=>{
        const id = parseInt(b.dataset.id);
        if (b.dataset.act === 'edit') openIngredientModal(ingredients.find(x=>x.id===id));
        else {
            if (!confirm('¿Eliminar ingrediente?')) return;
            const res = await fetch(`${API_BASE}/api/ingredients/${id}`, { method: 'DELETE' });
            if (res.ok) { showToast('Ingrediente eliminado'); loadGestor(); loadRecipes(); }
            else {
                const txt = await res.text();
                alert(txt || 'No se puede eliminar (puede estar en uso)');
            }
        }
    }));
}

// recipe modal
const recipeModal = document.getElementById('recipe-modal');
const recipeModalBackdrop = document.getElementById('recipe-modal-backdrop');
const recipeModalClose = document.getElementById('recipe-modal-close');
const recipeModalTitle = document.getElementById('recipe-modal-title');
const recipeId = document.getElementById('recipe-id');
const recipeName = document.getElementById('recipe-name');
const recipeServings = document.getElementById('recipe-servings');
const recipeInstructions = document.getElementById('recipe-instructions');
const recipeImageDrop = document.getElementById('recipe-image-drop');
const recipeImageFile = document.getElementById('recipe-image-file');
const recipeImageUrl = document.getElementById('recipe-image-url');
const recipeImagePreview = document.getElementById('recipe-image-preview');
const recipeIngList = document.getElementById('recipe-ingredients-list');
const btnAddRecipeIng = document.getElementById('btn-add-recipe-ing');
const btnNewIngInline = document.getElementById('btn-new-ing-inline');
const recipeSave = document.getElementById('recipe-save');
const recipeCancel = document.getElementById('recipe-cancel');
let recipeIngRows = [];
let recipeImageUploaded = '';

recipeModalBackdrop.addEventListener('click', closeRecipeModal);
recipeModalClose.addEventListener('click', closeRecipeModal);
recipeCancel.addEventListener('click', closeRecipeModal);
function closeRecipeModal(){ recipeModal.classList.remove('active'); document.body.style.overflow=''; }

recipeImageDrop.addEventListener('click', ()=>recipeImageFile.click());
recipeImageDrop.addEventListener('dragover', e=>{e.preventDefault();});
recipeImageDrop.addEventListener('drop', async e=>{e.preventDefault(); const f=e.dataTransfer.files[0]; if(f) await uploadImage(f, 'recipe'); });
recipeImageFile.addEventListener('change', async e=>{ const f=e.target.files[0]; if(f) await uploadImage(f, 'recipe'); });
recipeImageUrl.addEventListener('input', ()=>{ if(recipeImageUrl.value){ recipeImagePreview.src=recipeImageUrl.value; recipeImagePreview.style.display='block'; recipeImageUploaded=''; }});

async function uploadImage(file, type) {
    const fd = new FormData(); fd.append('file', file); fd.append('type', type);
    try {
        const res = await fetch(`${API_BASE}/api/images/upload`, { method: 'POST', body: fd });
        if (!res.ok) throw new Error();
        const data = await res.json();
        recipeImageUploaded = data.url;
        recipeImagePreview.src = API_BASE + '/' + data.url;
        recipeImagePreview.style.display = 'block';
        recipeImageUrl.value = '';
        showToast('Imagen subida');
    } catch(e) { alert('Error al subir imagen'); }
}

function openRecipeModal(detail) {
    recipeId.value = detail?.id || '';
    recipeModalTitle.textContent = detail ? 'Editar receta' : 'Nueva receta';
    recipeName.value = detail?.name || '';
    recipeServings.value = detail?.servings || 2;
    recipeInstructions.value = detail?.instructions || '';
    recipeImageUploaded = detail?.imageUrl || '';
    recipeImageUrl.value = detail?.imageUrl && detail.imageUrl.startsWith('http') ? detail.imageUrl : '';
    if (recipeImageUploaded) { recipeImagePreview.src = recipeImageUploaded.startsWith('http') ? recipeImageUploaded : API_BASE + '/' + recipeImageUploaded; recipeImagePreview.style.display = 'block'; } else recipeImagePreview.style.display = 'none';
    recipeIngRows = [];
    recipeIngList.innerHTML = '';
    if (detail?.ingredients) {
        for (const ing of detail.ingredients) {
            addRecipeIngRow({ ingredientId: ing.ingredientId, quantity: ing.quantity, unit: ing.unit });
        }
    } else {
        addRecipeIngRow({});
    }
    recipeModal.classList.add('active'); document.body.style.overflow = 'hidden';
}

function addRecipeIngRow(data) {
    const row = document.createElement('div'); row.className = 'recipe-ing-row';
    const sel = document.createElement('select'); sel.className = 'form__input';
    sel.innerHTML = '<option value="">Seleccionar ingrediente</option>' + ingredients.map(i=>`<option value="${i.id}" data-unit="${i.unit}">${i.name}</option>`).join('');
    if (data.ingredientId) sel.value = data.ingredientId;
    const qty = document.createElement('input'); qty.className = 'form__input'; qty.type='number'; qty.min='0'; qty.step='0.01'; qty.value = data.quantity || '';
    const presenceLabel = document.createElement('label'); presenceLabel.className = 'recipe-ing-presence';
    const presence = document.createElement('input'); presence.type = 'checkbox'; presence.checked = Number(data.quantity) > 0;
    presenceLabel.append(presence, document.createTextNode(' Está'));
    const unit = document.createElement('span'); unit.className = 'recipe-ing-unit'; unit.textContent = data.unit ? unitDisplay(data.unit) : '';
    const updateQuantityInput = (unitName) => {
        const isBinary = isBinaryUnit(unitName);
        qty.style.display = isBinary ? 'none' : '';
        presenceLabel.style.display = isBinary ? 'inline-flex' : 'none';
        unit.textContent = unitName ? unitDisplay(unitName) : '';
    };
    sel.addEventListener('change', ()=>{ const opt = sel.options[sel.selectedIndex]; updateQuantityInput(opt.dataset.unit); });
    updateQuantityInput(data.unit || sel.options[sel.selectedIndex]?.dataset.unit);
    const rm = document.createElement('button'); rm.className='form__link'; rm.textContent='Quitar';
    rm.addEventListener('click', ()=>{ row.remove(); recipeIngRows = recipeIngRows.filter(x=>x.row!==row); });
    row.appendChild(sel); row.appendChild(qty); row.appendChild(presenceLabel); row.appendChild(unit); row.appendChild(rm);
    recipeIngList.appendChild(row);
    recipeIngRows.push({ row, sel, qty, presence, unit });
}

btnAddRecipeIng.addEventListener('click', ()=>addRecipeIngRow({}));

function isBinaryUnit(unit){ return unit === 'Binary'; }
function unitDisplay(u){ const map={'Gram':'g','Milliliter':'ml','Unit':'unidad','Tablespoon':'cucharada','Teaspoon':'cucharadita','Binary':'presente'}; if(u==='Unit') return 'unidad'; return map[u]||u; }
function formatIngredientAmount(item){ return isBinaryUnit(item.unit) ? item.unitDisplay : `${item.quantity} ${item.unitDisplay}`; }

btnNewIngInline.addEventListener('click', ()=>{ openInlineIngModal(); });

recipeSave.addEventListener('click', async ()=>{
    const items = [];
    for (const r of recipeIngRows) {
        const id = r.sel.value ? parseInt(r.sel.value) : null;
        const unit = r.sel.options[r.sel.selectedIndex]?.dataset.unit;
        if (id && isBinaryUnit(unit) && r.presence.checked) items.push({ ingredientId: id, quantity: 1 });
        const q = parseFloat(r.qty.value);
        if (id && !isBinaryUnit(unit) && q > 0) items.push({ ingredientId: id, quantity: q });
    }
    if (items.length === 0) { alert('Agregá al menos un ingrediente'); return; }
    const img = recipeImageUploaded || (recipeImageUrl.value.startsWith('http') ? recipeImageUrl.value : recipeImageUrl.value || null);
    const payload = { name: recipeName.value, servings: parseInt(recipeServings.value), instructions: recipeInstructions.value || null, ingredients: items, imageUrl: img };
    try {
        let res;
        if (recipeId.value) res = await fetch(`${API_BASE}/api/recipes/${recipeId.value}`, { method:'PUT', headers:{'Content-Type':'application/json'}, body: JSON.stringify(payload) });
        else res = await fetch(`${API_BASE}/api/recipes`, { method:'POST', headers:{'Content-Type':'application/json'}, body: JSON.stringify(payload) });
        if (!res.ok) { const t=await res.text(); alert(t); return; }
        showToast('Receta guardada'); closeRecipeModal(); loadGestor(); loadRecipes();
    } catch(e) { alert('Error al guardar'); }
});

// ingredient modal
const ingredientModal = document.getElementById('ingredient-modal');
const ingredientModalBackdrop = document.getElementById('ingredient-modal-backdrop');
const ingredientModalClose = document.getElementById('ingredient-modal-close');
const ingredientModalTitle = document.getElementById('ingredient-modal-title');
const ingredientIdEl = document.getElementById('ingredient-id');
const ingredientName = document.getElementById('ingredient-name');
const ingredientUnit = document.getElementById('ingredient-unit');
const ingredientImageDrop = document.getElementById('ingredient-image-drop');
const ingredientImageFile = document.getElementById('ingredient-image-file');
const ingredientImageUrl = document.getElementById('ingredient-image-url');
const ingredientImagePreview = document.getElementById('ingredient-image-preview');
const ingredientSave = document.getElementById('ingredient-save');
const ingredientCancel = document.getElementById('ingredient-cancel');
let ingImageUploaded = '';

const unitOptions = [
    {v:'Gram',t:'gramo (g)'},{v:'Milliliter',t:'mililitro (ml)'},{v:'Unit',t:'unidad/unidades'},{v:'Tablespoon',t:'cucharada/s'},{v:'Teaspoon',t:'cucharadita/s'},{v:'Binary',t:'presente o ausente'}
];
ingredientUnit.innerHTML = unitOptions.map(o=>`<option value="${o.v}">${o.t}</option>`).join('');

ingredientModalBackdrop.addEventListener('click', closeIngredientModal);
ingredientModalClose.addEventListener('click', closeIngredientModal);
ingredientCancel.addEventListener('click', closeIngredientModal);
function closeIngredientModal(){ ingredientModal.classList.remove('active'); document.body.style.overflow=''; }

ingredientImageDrop.addEventListener('click', ()=>ingredientImageFile.click());
ingredientImageDrop.addEventListener('dragover', e=>e.preventDefault());
ingredientImageDrop.addEventListener('drop', async e=>{e.preventDefault(); const f=e.dataTransfer.files[0]; if(f) await uploadIngImage(f); });
ingredientImageFile.addEventListener('change', async e=>{ const f=e.target.files[0]; if(f) await uploadIngImage(f); });
ingredientImageUrl.addEventListener('input', ()=>{ if(ingredientImageUrl.value){ ingredientImagePreview.src=ingredientImageUrl.value; ingredientImagePreview.style.display='block'; ingImageUploaded=''; }});

async function uploadIngImage(file) {
    const fd = new FormData(); fd.append('file', file); fd.append('type', 'ingredient');
    try {
        const res = await fetch(`${API_BASE}/api/images/upload`, { method: 'POST', body: fd });
        if (!res.ok) throw new Error();
        const data = await res.json();
        ingImageUploaded = data.url;
        ingredientImagePreview.src = API_BASE + '/' + data.url;
        ingredientImagePreview.style.display = 'block';
        ingredientImageUrl.value = '';
        showToast('Imagen subida');
    } catch(e) { alert('Error al subir imagen'); }
}

function openIngredientModal(detail) {
    ingredientIdEl.value = detail?.id || '';
    ingredientModalTitle.textContent = detail ? 'Editar ingrediente' : 'Nuevo ingrediente';
    ingredientName.value = detail?.name || '';
    ingredientUnit.value = detail?.unit || 'Gram';
    ingImageUploaded = detail?.imageUrl || '';
    ingredientImageUrl.value = detail?.imageUrl && detail.imageUrl.startsWith('http') ? detail.imageUrl : '';
    if (ingImageUploaded) { ingredientImagePreview.src = ingImageUploaded.startsWith('http') ? ingImageUploaded : API_BASE + '/' + ingImageUploaded; ingredientImagePreview.style.display = 'block'; } else ingredientImagePreview.style.display = 'none';
    ingredientModal.classList.add('active'); document.body.style.overflow = 'hidden';
}

ingredientSave.addEventListener('click', async ()=>{
    const img = ingImageUploaded || (ingredientImageUrl.value.startsWith('http') ? ingredientImageUrl.value : ingredientImageUrl.value || null);
    const payload = { name: ingredientName.value, unit: ingredientUnit.value, imageUrl: img };
    try {
        let res;
        if (ingredientIdEl.value) res = await fetch(`${API_BASE}/api/ingredients/${ingredientIdEl.value}`, { method:'PUT', headers:{'Content-Type':'application/json'}, body: JSON.stringify(payload) });
        else res = await fetch(`${API_BASE}/api/ingredients`, { method:'POST', headers:{'Content-Type':'application/json'}, body: JSON.stringify(payload) });
        if (!res.ok) { const t=await res.text(); alert(t); return; }
        showToast('Ingrediente guardado'); closeIngredientModal(); loadGestor(); loadRecipes();
    } catch(e) { alert('Error al guardar'); }
});

// inline ingredient
const inlineIngModal = document.getElementById('inline-ing-modal');
const inlineIngBackdrop = document.getElementById('inline-ing-backdrop');
const inlineIngClose = document.getElementById('inline-ing-close');
const inlineIngName = document.getElementById('inline-ing-name');
const inlineIngUnit = document.getElementById('inline-ing-unit');
const inlineIngUrl = document.getElementById('inline-ing-url');
const inlineIngSave = document.getElementById('inline-ing-save');
const inlineIngCancel = document.getElementById('inline-ing-cancel');
inlineIngUnit.innerHTML = unitOptions.map(o=>`<option value="${o.v}">${o.t}</option>`).join('');
inlineIngBackdrop.addEventListener('click', closeInlineModal);
inlineIngClose.addEventListener('click', closeInlineModal);
inlineIngCancel.addEventListener('click', closeInlineModal);
function closeInlineModal(){ inlineIngModal.classList.remove('active'); document.body.style.overflow=''; }
function openInlineIngModal(){ inlineIngName.value=''; inlineIngUrl.value=''; inlineIngModal.classList.add('active'); document.body.style.overflow='hidden'; }
inlineIngSave.addEventListener('click', async ()=>{
    const payload = { name: inlineIngName.value, unit: inlineIngUnit.value, imageUrl: inlineIngUrl.value || null };
    try {
        const res = await fetch(`${API_BASE}/api/ingredients`, { method:'POST', headers:{'Content-Type':'application/json'}, body: JSON.stringify(payload) });
        if (!res.ok) { const t=await res.text(); alert(t); return; }
        const ing = await res.json();
        showToast('Ingrediente creado');
        closeInlineModal();
        await loadGestor();
        // add row and select
        addRecipeIngRow({ ingredientId: ing.id });
        // select last
        const last = recipeIngRows[recipeIngRows.length-1];
        if (last) { last.sel.value = ing.id; const opt = last.sel.options[last.sel.selectedIndex]; last.unit.textContent = opt.dataset.unit ? unitDisplay(opt.dataset.unit) : unitDisplay(ing.unit); }
    } catch(e) { alert('Error'); }
});

function populateRecipeIngSelects(){ /* no-op */ }

loadRecipes();
applyRoute();
