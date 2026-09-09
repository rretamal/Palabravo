# Palabravo — Resumen del MVP

## 1. Concepto

**Palabravo** es un juego móvil de palabras en español centrado en **descubrir relaciones entre palabras**, progresar por rangos y volver cada día por un nuevo reto.

La propuesta combina:

- Puzzle de **categorías / grouping**.
- Progresión tipo **Road to Grandmaster**.
- Medallas y rangos.
- Desafío diario.
- Ranking.
- Resultados compartibles.
- Retos asíncronos entre amigos.
- Monetización híbrida mediante anuncios opcionales + pago único para quitar anuncios.

La idea no es hacer “otro juego de palabras”, sino convertir el dominio de palabras en una **experiencia de progreso, maestría y competencia ligera**.

## 2. Mecánica principal

Cada puzzle presenta **16 palabras**.

El jugador debe descubrir **4 grupos de 4 palabras relacionadas**.

Ejemplo:

- Marte
- Venus
- Saturno
- Neptuno

→ **Planetas**

Otro grupo podría ser:

- Guitarra
- Violín
- Arpa
- Violonchelo

→ **Instrumentos de cuerda**

El jugador selecciona cuatro palabras y confirma su elección.

- Si el grupo es correcto, queda resuelto.
- Si es incorrecto, pierde un intento.
- El puzzle termina cuando se encuentran los cuatro grupos o se agotan los intentos.

La dificultad aumenta introduciendo palabras ambiguas que podrían parecer pertenecer a más de un grupo.

## 3. Loop principal

```text
Resolver
  ↓
Ganar medalla
  ↓
Avanzar en el camino
  ↓
Subir de rango
  ↓
Comparar resultado
  ↓
Compartir / desafiar
  ↓
Volver al siguiente reto o al Daily
```

El objetivo emocional no es solamente “resolver un puzzle”, sino:

> **Sentir que estoy mejorando y avanzando como jugador.**

## 4. Progresión

El MVP tendrá **30 desafíos fijos**, ordenados por dificultad creciente.

Ejemplo de rangos:

1. Novato
2. Ágil
3. Ingenioso
4. Experto
5. Maestro
6. Gran Maestro

Cada rango contiene varios desafíos.

Los primeros niveles funcionan casi como onboarding, mientras que los últimos deben incluir más falsos amigos, ambigüedad y relaciones lingüísticas menos obvias.

## 5. Medallas

Cada desafío puede entregar:

- 🥉 **Bronce**
- 🥈 **Plata**
- 🥇 **Oro**

La medalla debe depender principalmente de:

- número de errores,
- cantidad de pistas utilizadas,
- calidad de resolución.

El tiempo puede utilizarse como criterio secundario o desempate, pero no debería ser el único factor.

### Experiencia visual

Al completar un puzzle:

1. se muestra brevemente el resultado;
2. la app vuelve al mapa;
3. la medalla se anima hacia el nodo correspondiente;
4. avanza la barra del rango;
5. se indica cuánto falta para el siguiente rango.

Las celebraciones grandes se reservan para:

- subir de rango;
- completar una etapa;
- conseguir un logro especial;
- superar un récord.

## 6. Desafío diario

Además del camino permanente existirá un **Daily Challenge**.

Características:

- mismo puzzle para todos ese día;
- una oportunidad clara de volver diariamente;
- ranking semanal;
- resultado compartible sin spoilers;
- posibilidad de comparar desempeño.

El Daily busca crear:

```text
hábito + competencia ligera + sharing
```

## 7. Social y desafíos a amigos

El jugador podrá compartir su resultado o desafiar directamente a otra persona.

Ejemplo:

> Ricardo resolvió el reto en 2:14 sin errores.  
> ¿Puedes superarlo?

El reto será **asíncrono**: no requiere que ambos jugadores estén conectados simultáneamente.

Flujo ideal:

```text
Jugador completa reto
  ↓
Compartir
  ↓
WhatsApp / red social
  ↓
Amigo abre enlace
  ↓
Abre app o instala
  ↓
Juega el mismo desafío
  ↓
Comparación
  ↓
Revancha
```

No se construirá inicialmente:

- sistema interno de amigos;
- chat;
- presencia online;
- matchmaking;
- PvP en tiempo real;
- clanes.

## 8. Contenido inicial

El MVP tendrá:

- **30 puzzles principales** de progresión;
- puzzles para el Daily;
- contenido curado manualmente.

No se dependerá inicialmente de generación automática masiva.

La calidad del contenido es parte central del producto.

Los puzzles deben:

- ser correctos en español;
- evitar categorías demasiado subjetivas;
- tener dificultad progresiva;
- incluir ambigüedad deliberada en niveles avanzados;
- ser revisados para España y Latinoamérica.

Es preferible tener **30 puzzles excelentes** que cientos de puzzles mediocres.

## 9. Dirección visual

La dirección recomendada es:

> **Gameplay calmado + meta game expresivo y celebratorio.**

### Gameplay

Debe sentirse:

- limpio;
- claro;
- adulto;
- legible;
- poco distractor.

Base visual:

- marfil / crema;
- tarjetas claras;
- mucho espacio;
- tipografía muy legible.

### Meta game

Home, perfil, ranking, medallas y resultados pueden tener más personalidad:

- coral / terracota como color principal;
- mostaza;
- verde salvia;
- azul suave;
- formas redondeadas;
- ilustración puntual;
- medallas más expresivas;
- pequeñas animaciones.

La personalidad debería ser:

- inteligente;
- cálida;
- moderna;
- celebratoria;
- ligeramente competitiva;
- nunca infantil ni estilo eSport.

## 10. Pantallas principales del MVP

### Onboarding
Explica en pocos segundos:

> Cuatro grupos.  
> Dieciséis palabras.

Debe permitir empezar sin registro.

### Inicio
Contendrá principalmente:

- Daily Challenge;
- racha actual;
- progreso en el camino;
- acceso al siguiente reto.

### Juego
Contiene:

- 16 palabras;
- intentos restantes;
- selección de cuatro palabras;
- pistas;
- feedback al acertar un grupo.

### Camino
Muestra:

- 30 nodos;
- medallas;
- rangos;
- retos bloqueados/desbloqueados;
- progreso hacia el siguiente rango.

### Perfil
Debe ser una pantalla de **progreso**, no un dashboard administrativo.

Incluye:

- rango actual;
- progreso al siguiente rango;
- racha;
- puzzles resueltos;
- porcentaje perfecto;
- medallero;
- acceso a ranking;
- cuenta opcional;
- remove ads.

### Ranking
Principalmente para Daily y/o ranking semanal.

### Resultado
Resumen breve con:

- medalla;
- errores;
- tiempo;
- posición/ranking si aplica;
- compartir;
- siguiente desafío.

## 11. Registro

El usuario debe poder completar su primera experiencia **sin crear una cuenta**.

El registro se solicitará solamente cuando tenga una razón real:

- guardar progreso;
- sincronizar dispositivos;
- aparecer persistentemente en rankings;
- recuperar historial.

Principio:

> **Primero jugar. Después pedir compromiso.**

## 12. Monetización

Modelo híbrido simple.

### Rewarded Ads

Usados para beneficios voluntarios:

- pista;
- revelar información parcial;
- ayuda en un puzzle.

### Interstitial

Solamente entre desafíos y con frecuencia moderada.

Nunca interrumpir un puzzle.

### IAP

Compra única:

> **Palabravo sin anuncios**

Precio inicial tentativo:

**US$2.99**

No se justifica inicialmente:

- suscripción;
- monedas complejas;
- battle pass;
- tienda de skins.

## 13. Analytics

Instrumentar desde el primer lanzamiento.

### Activación

- tutorial_started
- tutorial_completed
- puzzle_started
- puzzle_completed
- puzzle_abandoned

### Progresión

- challenge_started
- challenge_completed
- medal_earned
- rank_up

### Retención

- D1
- D3
- D7
- D14
- D28
- sesiones por DAU

### Daily

- daily_started
- daily_completed
- daily_return

### Social

- share_prompt_shown
- share_started
- share_completed
- challenge_created
- challenge_opened
- referred_install
- referred_player_activated
- rematch_started

### Monetización

- rewarded_offer
- rewarded_started
- rewarded_completed
- interstitial_impression
- remove_ads_viewed
- remove_ads_purchased

## 14. Métricas principales

El MVP no se evaluará inicialmente por ingresos.

Las preguntas son:

1. **¿La gente completa el primer puzzle?**
2. **¿La gente vuelve?**
3. **¿La gente avanza por el camino?**
4. **¿La gente utiliza el Daily?**
5. **¿La gente comparte?**
6. **¿Los shares generan nuevos jugadores?**

Referencias iniciales para decidir:

| Métrica | Débil | Viable | Buena señal |
|---|---:|---:|---:|
| Primer puzzle completado | <60% | 60–75% | >75% |
| D1 | <20% | 20–28% | >28–30% |
| D7 | <4% | 4–8% | >8% |
| D28 | <2% | 2–4% | >4% |
| Sesiones / DAU | <1.5 | 1.5–2.5 | >2.5 |

El sharing debe medirse como funnel real, no solamente como número de taps.

## 15. Kill criteria

No agregar más features automáticamente si las métricas son malas.

### Señales para parar o replantear

- D1 claramente por debajo de ~20%.
- D7 claramente por debajo de ~4%.
- usuarios no regresan al Daily;
- casi nadie comparte;
- los shares no producen aperturas o instalaciones;
- el Store Listing no consigue orgánico después de probar creatividades razonables.

La respuesta a una señal débil **no debe ser construir cientos de niveles adicionales**.

## 16. Fuera del MVP

No construir inicialmente:

- multiplayer en tiempo real;
- clanes;
- chat;
- sistema complejo de amigos;
- ligas avanzadas;
- seasons;
- battle pass;
- monedas;
- skins;
- avatars complejos;
- cientos de puzzles;
- múltiples mecánicas de palabras;
- backend social grande.

## 17. Timebox

Objetivo:

**5–9 días de desarrollo enfocado + QA de contenido.**

La primera versión debe ser suficientemente buena para validar la hipótesis, no para competir feature por feature con grandes publishers.

Al publicar:

> **dejar de agregar features y medir.**

## 18. Hipótesis principal

> **¿Un juego de categorías de palabras en español, convertido en un camino de maestría con medallas, desafío diario, ranking y sharing, consigue suficiente retención y reproducción orgánica como para justificar seguir invirtiendo?**

## 19. Posicionamiento resumido

No:

> “Otro juego de palabras.”

Sí:

> **Descubre conexiones. Domina los retos. Sube de rango.**

O, conceptualmente:

> **El camino para convertirte en maestro de las palabras.**

## 20. Alcance final del MVP

```text
✓ 1 mecánica: 16 palabras → 4 grupos
✓ 30 desafíos curados
✓ Camino de progresión
✓ 6 rangos
✓ Bronce / Plata / Oro
✓ Daily Challenge
✓ Ranking
✓ Sharing sin spoilers
✓ Friend Challenge asíncrono
✓ Rewarded hint
✓ Interstitial moderado
✓ Remove Ads IAP
✓ Analytics
✓ Jugar sin registro

✗ Multiplayer
✗ Clanes
✗ Chat
✗ Battle Pass
✗ Seasons complejas
✗ Múltiples minijuegos
✗ Cientos de niveles
```

La prioridad es validar **retención + hábito diario + sharing**, no maximizar funcionalidades ni ingresos desde el primer lanzamiento.
