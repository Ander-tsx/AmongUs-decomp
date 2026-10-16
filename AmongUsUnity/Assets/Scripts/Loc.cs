using System.Collections.Generic;
using UnityEngine;

namespace AmongUsLan
{
    /// <summary>Tiny two-language (en/es) string table. English text is the key.</summary>
    public static class Loc
    {
        public static bool Spanish = true;     // default UI language (toggle in the menu)

        static readonly Dictionary<string, string> Es = new Dictionary<string, string>
        {
            // menu / lobby
            { "Create Game", "Crear partida" }, { "Join", "Unirse" }, { "Join by IP", "Unirse por IP" }, { "Name", "Nombre" },
            { "Games on the local network", "Partidas en la red local" }, { "Searching...", "Buscando..." }, { "players", "jugadores" },
            { "Move: WASD/arrows   Use: E   Kill: Q   Report: R   Vent: V", "Mover: WASD/flechas   Usar: E   Matar: Q   Reportar: R   Ventilar: V" },
            { "Your LAN address", "Tu dirección LAN" }, { "Waiting for the host", "Esperando al anfitrión" }, { "START", "EMPEZAR" }, { "START (solo)", "EMPEZAR (solo)" },
            { "Choose your colour", "Elige tu color" }, { "Close", "Cerrar" }, { "Connecting...", "Conectando..." },
            { "Connection lost", "Se perdió la conexión con el anfitrión" }, { "Game in progress", "Partida en curso" }, { "Room full", "Sala llena" },
            // hud
            { "TOTAL TASKS COMPLETED", "TAREAS COMPLETADAS" }, { "Fake Tasks:", "Tareas falsas:" }, { "YOU ARE DEAD", "ESTÁS MUERTO" }, { "Impostors: ", "Impostores: " },
            // roles / intro
            { "Crewmate", "Tripulante" }, { "Impostor", "Impostor" }, { "Sabotage and kill everyone", "Sabotea y mata a todos" },
            { "Kill all Crewmates with your fellow Impostors", "Mata a todos los tripulantes junto a tus socios" },
            { "There is 1 Impostor among us", "Hay 1 Impostor entre nosotros" }, { "There are {0} Impostors among us", "Hay {0} Impostores entre nosotros" },
            // meeting
            { "Who died?", "¿Quién murió?" }, { "Who is the Impostor?", "¿Quién es el Impostor?" }, { "Voting Ends: ", "La votación termina: " }, { "Skip Vote", "Saltar voto" },
            { " was an Impostor.", " era un Impostor." }, { " was not an Impostor.", " no era un Impostor." }, { "No one was ejected.", "Nadie fue expulsado." },
            { " Impostor remains.", " Impostor queda." }, { " Impostors remain.", " Impostores quedan." },
            { "Crewmates Win", "¡Ganan los Tripulantes!" }, { "Impostors Win", "¡Ganan los Impostores!" }, { "Back to Lobby", "Volver a la sala" }, { "Waiting for the host...", "Esperando al anfitrión..." },
            // sabotage
            { "REACTOR MELTDOWN", "FUSIÓN DEL REACTOR" }, { "OXYGEN DEPLETED", "OXÍGENO AGOTÁNDOSE" }, { "LIGHTS SABOTAGED", "LUCES SABOTEADAS" }, { "COMMS SABOTAGED", "COMUNICACIONES SABOTEADAS" },
            { "Reactor", "Reactor" }, { "O2", "O2" }, { "Lights", "Luces" }, { "Comms", "Comunicaciones" }, { "Doors >", "Puertas >" }, { "< Back", "< Atrás" }, { "Doors: ", "Puertas: " },
            { "Impostors", "Impostores" }, { "Player speed", "Velocidad" }, { "Kill cooldown", "Enfriamiento de muerte" }, { "Kill distance", "Distancia de muerte" },
            { "Short", "Corta" }, { "Normal", "Normal" }, { "Long", "Larga" }, { "Crew vision", "Visión tripulantes" }, { "Impostor vision", "Visión impostores" },
            { "Common tasks", "Tareas comunes" }, { "Short tasks", "Tareas cortas" }, { "Long tasks", "Tareas largas" }, { "Emergency meetings", "Reuniones de emergencia" },
            { "Voting time", "Tiempo de votación" }, { "Anonymous votes", "Votos anónimos" }, { "On", "Sí" }, { "Off", "No" }, { "Game Settings", "Ajustes de partida" },
            { "TITLE", "HAY UN IMPOSTOR ENTRE NOSOTROS" }, { "Room code", "Código de sala" }, { "Code", "Código" }, { "Join with code", "Unirse con código" },
            { "DEAD BODY REPORTED", "¡CUERPO REPORTADO!" }, { "EMERGENCY MEETING", "REUNIÓN DE EMERGENCIA" },
            { "VICTORY", "VICTORIA" }, { "Move to a vent", "Moverse a otra ventila" }, { "A sabotage is already active", "Ya hay un sabotaje activo" }, { "Recharging...", "Recargando..." }, { "DEFEAT", "DERROTA" },
            // tasks
            { "Swipe Card", "Deslizar tarjeta" }, { "Fix Wiring", "Arreglar cableado" }, { "Empty Garbage", "Vaciar basura" }, { "Calibrate Distributor", "Calibrar distribuidor" },
            { "Chart Course", "Trazar curso" }, { "Clean O2 Filter", "Limpiar filtro de O2" }, { "Prime Shields", "Preparar escudos" }, { "Stabilize Steering", "Estabilizar dirección" },
            { "Unlock Manifolds", "Desbloquear colectores" }, { "Submit Scan", "Enviar escaneo" }, { "Clear Asteroids", "Destruir asteroides" },
            { "Align Engine Output", "Alinear salida del motor" }, { "Fuel Engines", "Repostar motores" }, { "Start Reactor", "Iniciar reactor" },
            { "Inspect Sample", "Inspeccionar muestra" }, { "Upload Data", "Subir datos" }, { "Divert Power", "Desviar energía" },
            { "Navigation", "Navegación" }, { "Cafeteria", "Cafetería" }, { "Electrical", "Eléctrico" }, { "Storage", "Almacén" }, { "Admin", "Admin" },
            { "Upper Engine", "Motor superior" }, { "Lower Engine", "Motor inferior" }, { "MedBay", "Enfermería" }, { "Security", "Seguridad" }, { "Weapons", "Armas" },
            { "Shields", "Escudos" }, { "Communications", "Comunicaciones" },
        };

        public static string T(string en)
        {
            if (en == "TITLE" && !Spanish) return "THERE IS AN IMPOSTOR AMONG US";
            if (Spanish && Es.TryGetValue(en, out var es)) return es;
            return en;
        }

        public static string F(string en, params object[] args) => string.Format(T(en), args);
    }
}
