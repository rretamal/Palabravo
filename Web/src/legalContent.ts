import type { Locale } from './content'

export type LegalKind = 'privacy' | 'terms'

export interface LegalSection {
  title: string
  paragraphs?: string[]
  items?: string[]
}

export interface LegalDocument {
  eyebrow: string
  title: string
  intro: string
  updated: string
  sections: LegalSection[]
}

export const legalDocuments: Record<Locale, Record<LegalKind, LegalDocument>> = {
  es: {
    privacy: {
      eyebrow: 'Información legal', title: 'Política de privacidad', updated: 'Última actualización: 8 de septiembre de 2026',
      intro: 'Esta política explica qué información trata Palabravo, para qué se utiliza y cómo puedes solicitar su eliminación.',
      sections: [
        { title: 'Responsable y contacto', paragraphs: ['Palabravo es ofrecido por R. Retamal, Santiago, Chile. Para consultas de privacidad puedes escribir a hello@palabravo.app.'] },
        { title: 'Datos que tratamos', items: [
          'Un identificador aleatorio de PlayFab y un alias generado para crear la cuenta invitada y mostrar el ranking.',
          'Resultados del reto diario, puntajes y nombre visible necesarios para operar el ranking.',
          'Si eliges vincular Google Play Games: el código de autenticación temporal y la identidad de jugador que Google permite asociar con Palabravo.',
          'Progreso, medallas, rachas, tutorial y preferencias guardados localmente en tu dispositivo.',
          'Si solicitas borrado por la web: correo de contacto, identificador proporcionado, detalles opcionales y fecha de la solicitud.',
        ] },
        { title: 'Cómo usamos los datos', paragraphs: ['Usamos estos datos únicamente para ejecutar el juego, guardar el progreso local, operar el ranking, vincular opcionalmente Google Play Games, prevenir abuso y atender solicitudes de soporte o eliminación. Palabravo no vende datos personales ni incorpora publicidad basada en perfiles.'] },
        { title: 'Proveedores', items: [
          'Microsoft PlayFab aloja la cuenta invitada, las vinculaciones y los datos del ranking.',
          'Google Play Games procesa el inicio de sesión opcional conforme a sus propias políticas.',
          'Azure Static Web Apps y Azure Functions alojan este sitio y procesan las solicitudes técnicas de borrado.',
          'Resend entrega los correos de solicitudes manuales y sus confirmaciones.',
        ] },
        { title: 'Conservación y seguridad', paragraphs: ['Las comunicaciones usan conexiones cifradas. El progreso local permanece hasta que borres los datos, desinstales la app o restablezcas el dispositivo. Los datos de PlayFab se conservan mientras exista la cuenta. Las solicitudes manuales se atienden en un máximo de 30 días y sus correos se eliminan 30 días después de cerrar el caso, salvo obligación legal aplicable.'] },
        { title: 'Eliminación y tus derechos', paragraphs: ['Puedes solicitar la eliminación desde Perfil → Eliminar cuenta y datos. Si ya no tienes acceso a la app, usa la página Eliminar mis datos. La eliminación incluye la cuenta de Palabravo, datos alojados en PlayFab, resultados del ranking y vinculaciones con el juego. No elimina tu cuenta general ni tu perfil de Google Play Games. También puedes escribirnos para consultar, corregir o limitar el tratamiento de información que podamos identificar.'] },
        { title: 'Menores y cambios', paragraphs: ['Palabravo no está diseñado para recopilar conscientemente información de menores más allá de los datos técnicos necesarios para el juego. Podemos actualizar esta política si cambian las funciones o los proveedores; publicaremos aquí la fecha de la versión vigente.'] },
      ],
    },
    terms: {
      eyebrow: 'Información legal', title: 'Términos de uso', updated: 'Última actualización: 8 de septiembre de 2026',
      intro: 'Al instalar o utilizar Palabravo aceptas estos términos. Si no estás de acuerdo, no utilices la aplicación.',
      sections: [
        { title: 'El servicio', paragraphs: ['Palabravo es un juego de asociaciones de palabras en español que ofrece retos, un modo diario, progreso local y un ranking opcional. Algunas funciones requieren conexión a internet y servicios de terceros.'] },
        { title: 'Uso permitido', items: ['Utiliza la app de manera personal y lícita.', 'No intentes alterar puntajes, eludir límites, acceder a cuentas ajenas ni interferir con los servicios.', 'No automatices solicitudes ni uses el servicio de forma que perjudique a otros jugadores.'] },
        { title: 'Cuentas y disponibilidad', paragraphs: ['La app puede crear una cuenta invitada técnica y permite vincular Google Play Games. Eres responsable del acceso a tu dispositivo y a tu cuenta de Google. Podemos cambiar, suspender o retirar funciones para mantener la seguridad, corregir errores o evolucionar el juego.'] },
        { title: 'Contenido y propiedad intelectual', paragraphs: ['Palabravo, su identidad visual, textos editoriales, retos y software están protegidos por las leyes aplicables. Puedes compartir los resultados generados por la app para uso personal, pero no redistribuir el catálogo ni copiar sustancialmente el servicio.'] },
        { title: 'Sin garantías absolutas', paragraphs: ['Procuramos que el contenido y el servicio funcionen correctamente, pero no garantizamos disponibilidad ininterrumpida ni que todos los retos estén libres de errores. Palabravo no sustituye formación lingüística profesional.'] },
        { title: 'Responsabilidad y contacto', paragraphs: ['En la medida permitida por la ley, no somos responsables de pérdidas indirectas derivadas del uso o imposibilidad de uso del servicio. Nada en estos términos limita derechos irrenunciables del consumidor. Para consultas escribe a hello@palabravo.app.'] },
      ],
    },
  },
  en: {
    privacy: {
      eyebrow: 'Legal information', title: 'Privacy policy', updated: 'Last updated: September 8, 2026',
      intro: 'This policy explains what information Palabravo processes, why it is used, and how you can request its deletion.',
      sections: [
        { title: 'Controller and contact', paragraphs: ['Palabravo is offered by R. Retamal, Santiago, Chile. For privacy questions, contact hello@palabravo.app.'] },
        { title: 'Data we process', items: [
          'A random PlayFab identifier and generated alias used to create a guest account and display the leaderboard.',
          'Daily challenge results, scores, and display name required to operate the leaderboard.',
          'If you link Google Play Games: the temporary authentication code and player identity Google allows Palabravo to associate.',
          'Progress, medals, streaks, tutorial state, and preferences stored locally on your device.',
          'If you request deletion on the website: contact email, the identifier you provide, optional details, and request date.',
        ] },
        { title: 'How we use data', paragraphs: ['We use this data only to run the game, keep local progress, operate the leaderboard, optionally link Google Play Games, prevent abuse, and answer support or deletion requests. Palabravo does not sell personal data or include profile-based advertising.'] },
        { title: 'Service providers', items: [
          'Microsoft PlayFab hosts guest accounts, account links, and leaderboard data.',
          'Google Play Games processes optional sign-in under its own policies.',
          'Azure Static Web Apps and Azure Functions host this site and process technical deletion requests.',
          'Resend delivers manual deletion request emails and confirmations.',
        ] },
        { title: 'Retention and security', paragraphs: ['Communications use encrypted connections. Local progress remains until you delete data, uninstall the app, or reset the device. PlayFab data remains while the account exists. Manual requests are handled within 30 days, and request emails are deleted 30 days after the case is closed unless applicable law requires otherwise.'] },
        { title: 'Deletion and your rights', paragraphs: ['Request deletion from Profile → Delete account and data. If you no longer have the app, use the Delete my data page. Deletion covers the Palabravo account, PlayFab-hosted data, leaderboard results, and links to the game. It does not delete your general Google account or Google Play Games profile. You may also contact us to access, correct, or restrict information we can identify.'] },
        { title: 'Children and changes', paragraphs: ['Palabravo is not designed to knowingly collect information from children beyond technical data needed for the game. We may update this policy when features or providers change; the effective date will be shown here.'] },
      ],
    },
    terms: {
      eyebrow: 'Legal information', title: 'Terms of use', updated: 'Last updated: September 8, 2026',
      intro: 'By installing or using Palabravo, you agree to these terms. If you do not agree, do not use the application.',
      sections: [
        { title: 'The service', paragraphs: ['Palabravo is a Spanish word-association game with challenges, a daily mode, local progress, and an optional leaderboard. Some features require an internet connection and third-party services.'] },
        { title: 'Acceptable use', items: ['Use the app personally and lawfully.', 'Do not manipulate scores, bypass limits, access another account, or interfere with the services.', 'Do not automate requests or use the service in a way that harms other players.'] },
        { title: 'Accounts and availability', paragraphs: ['The app may create a technical guest account and lets you link Google Play Games. You are responsible for access to your device and Google account. We may change, suspend, or remove features to maintain security, fix problems, or evolve the game.'] },
        { title: 'Content and intellectual property', paragraphs: ['Palabravo, its visual identity, editorial text, challenges, and software are protected by applicable laws. You may share result cards generated by the app for personal use, but may not redistribute the catalog or substantially copy the service.'] },
        { title: 'No absolute warranties', paragraphs: ['We work to keep the content and service accurate, but cannot guarantee uninterrupted availability or that every challenge is error-free. Palabravo is not a substitute for professional language instruction.'] },
        { title: 'Liability and contact', paragraphs: ['To the extent permitted by law, we are not liable for indirect losses resulting from use or inability to use the service. Nothing in these terms limits non-waivable consumer rights. Contact hello@palabravo.app with questions.'] },
      ],
    },
  },
}
