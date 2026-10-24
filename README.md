# MealPreparator

Backend API para administrar ingredientes y recetas, y calcular la lista de compras necesaria para un meal prep semanal.

La aplicación permite elegir recetas con una cantidad de porciones y devuelve los ingredientes totales agregados. Por ejemplo, pedir 2 porciones de "Arroz con pollo" y 3 porciones de "Ensalada de frutas" devuelve una única lista con todos los ingredientes y cantidades necesarias.

## Estado

El repositorio contiene la v0 compilable y testeada. La migración inicial está generada y la base `data/mealpreparator.db` ya existe con los datos de ejemplo.

## Arquitectura

Se usa una separación en cuatro capas con dependencias en una sola dirección:

- `MealPreparator.Domain`: entidades, reglas de negocio y cálculo de la lista de compras.
- `MealPreparator.Application`: casos de uso, DTOs, validaciones y contratos de persistencia.
- `MealPreparator.Infrastructure`: EF Core, SQLite, repositorios y datos de ejemplo.
- `MealPreparator.Api`: endpoints HTTP, manejo de errores y configuración de la aplicación.

Los tests se dividen en unitarios para el dominio y de integración para los endpoints.

## Modelo de datos

- `Ingredient`: nombre, unidad canónica de medida y categoría omitida por ahora.
- `Recipe`: nombre, cantidad de porciones que rinde e instrucciones opcionales.
- `RecipeIngredient`: relación entre receta e ingrediente con cantidad.

Cada ingrediente tiene una única unidad canónica. No se realizan conversiones entre unidades: si un ingrediente se mide en gramos, todas las recetas lo usan en gramos.

La cantidad de una receta se escala según las porciones pedidas:

```text
factor = porciones pedidas / porciones que rinde la receta
cantidad final = cantidad de la receta * factor
```

## Requisitos

- .NET SDK 10 (el repositorio incluye `global.json`).

En NixOS se puede usar de forma temporal:

```bash
nix-shell -p dotnet-sdk_10
```

## Puesta en marcha

```bash
dotnet restore MealPreparator.slnx
dotnet tool restore
dotnet build MealPreparator.slnx
dotnet test MealPreparator.slnx
```

Generar una nueva migración cuando cambie el modelo:

```bash
dotnet ef migrations add NombreDeLaMigracion \
  --project src/MealPreparator.Infrastructure \
  --startup-project src/MealPreparator.Api \
  --output-dir Persistence/Migrations
```

Ejecutar la API:

```bash
dotnet run --project src/MealPreparator.Api
```

Al iniciar, la aplicación aplica las migraciones y carga los datos de ejemplo si la base está vacía. La API queda disponible en `http://localhost:5080`.

La especificación OpenAPI se expone en `/openapi/v1.json` en entorno de desarrollo.

## Endpoints

### Ingredientes

| Método | Ruta | Descripción |
| --- | --- | --- |
| GET | `/api/ingredients` | Lista ingredientes |
| GET | `/api/ingredients/{id}` | Obtiene un ingrediente |
| POST | `/api/ingredients` | Crea un ingrediente |
| PUT | `/api/ingredients/{id}` | Actualiza un ingrediente |
| DELETE | `/api/ingredients/{id}` | Elimina un ingrediente sin recetas asociadas |

### Recetas

| Método | Ruta | Descripción |
| --- | --- | --- |
| GET | `/api/recipes` | Lista recetas |
| GET | `/api/recipes/{id}` | Obtiene una receta con sus ingredientes |
| POST | `/api/recipes` | Crea una receta |
| PUT | `/api/recipes/{id}` | Actualiza una receta y sus ingredientes |
| DELETE | `/api/recipes/{id}` | Elimina una receta |

### Lista de compras

| Método | Ruta | Descripción |
| --- | --- | --- |
| POST | `/api/shopping-list` | Calcula los totales para una selección de recetas |

Ejemplo de request:

```json
{
  "items": [
    { "recipeId": 1, "portions": 2 },
    { "recipeId": 2, "portions": 3 }
  ]
}
```

Ejemplo de response:

```json
[
  {
    "ingredientId": 1,
    "ingredientName": "Arroz",
    "unit": "Gram",
    "quantity": 200
  }
]
```

Las cantidades se redondean a dos decimales en la respuesta.

## Base de datos

La base se guarda en `data/mealpreparator.db` y se versiona en el repositorio porque el uso es local y de un único usuario.

Los archivos `-wal` y `-shm` no se versionan. Para regenerar la base desde cero, se puede mover o eliminar el archivo `.db` y volver a iniciar la API para que se apliquen las migraciones y el seed.

## Datos de ejemplo

El seed incluye ingredientes básicos y dos recetas:

- Arroz con pollo, rinde 4 porciones.
- Ensalada de frutas, rinde 2 porciones.

## Decisiones y pendientes

- No hay autenticación ni multiusuario.
- No se persisten planes semanales: la lista se calcula por request.
- No hay conversión entre unidades.
- No hay categorías de ingredientes.
- El frontend queda para una etapa posterior.
- La base SQLite se versiona como archivo binario; las migraciones y el seed en código siguen siendo la fuente para reconstruirla.
