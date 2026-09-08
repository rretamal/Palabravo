# Palabravo — recursos para Google Play

Archivos listos para subir desde `final/`:

- `app-icon-512.png`: ícono común, 512 × 512.
- `feature-graphic-es-419.png`: gráfico destacado en español, 1024 × 500.
- `feature-graphic-en-US.png`: gráfico destacado en inglés, 1024 × 500.
- `phone-01-es-419.png` y `phone-02-es-419.png`: capturas 9:16 en español.
- `phone-01-en-US.png` y `phone-02-en-US.png`: capturas 9:16 para la ficha en inglés.

Las capturas promocionales usan una pantalla real proporcionada durante las pruebas y ajustan únicamente dos detalles que ya cambiaron en el código actual: tres intentos y palabras largas en una sola línea. Para una ficha más completa y elegible para promoción conviene ampliar este mínimo con cuatro pantallas reales diferentes: inicio, camino de retos, partida y resultado.

Ejecutar `build_assets.py` con el Python empaquetado del entorno de trabajo vuelve a generar el conjunto final desde los archivos de `source/`.
