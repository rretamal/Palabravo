# Nombre público y país

La app importa el nombre del jugador de Google Play Games al vincular la cuenta. Para cuentas ya vinculadas, lo intenta también al abrir Perfil y al obtener la sesión para rankings. Una edición manual posterior conserva prioridad sobre el nombre importado. Si Google no responde, se conserva el alias existente y se puede editar.

Perfil permite un nombre de 1 a 24 caracteres y un país opcional elegido de la lista del sistema. La opción inicial es Sin bandera. No se infiere el país del idioma ni de la ubicación. Estas preferencias se guardan localmente y se eliminan al borrar la cuenta.

Camino y Semanal reciben el nombre público con la bandera elegida como prefijo. La API actualiza ese nombre aunque el puntaje recibido sea igual o menor, conservando el mejor resultado completo y su fecha. Los resultados anteriores sin bandera siguen siendo compatibles. Publicar juntos el binario y la API para habilitar las actualizaciones de nombres existentes.

La integración del nombre de Play Games requiere validación en un dispositivo Android autorizado. El SDK Java incluye PlayersClient, pero la versión instalada del binding .NET no lo expone; el adaptador utiliza los métodos públicos Java mediante reflexión.
