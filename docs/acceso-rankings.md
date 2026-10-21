# Acceso a rankings — compilación 8

El ranking del camino se muestra después de completar al menos un reto (camino, diario o semanal). El tutorial o una partida incompleta no lo desbloquean. Se reconocen las finalizaciones existentes, incluidas las de ediciones anteriores.

El ranking semanal requiere completar el puzzle del evento vigente. Una finalización de otra semana no sirve. Cualquier medalla obtenida al completar el reto permite entrar; no se exige oro, vinculación con Google ni compra.

Antes de participar, la app muestra una invitación con un botón al camino o al evento semanal correspondiente. No solicita la clasificación remota mientras está bloqueada, y limpia filas y posición anteriores al cargar o cambiar de modo. Al volver de jugar se evalúa el progreso nuevamente. Los resultados pendientes de sincronización permiten entrar y conservan el aviso de reintento existente.

Es una regla de participación de la interfaz de la app. No convierte los endpoints públicos de clasificación en información privada ni modifica su autenticación.

Validación: 113 pruebas .NET aprobadas, incluidas cinco nuevas comprobaciones de primer uso, modalidades y cambio de semana. Compilación Android Release verificada por separado. La prueba visual en el teléfono requiere distribuir la compilación 8 con la firma habitual de Google Play; no se desinstala ni borra el progreso para probarla.
