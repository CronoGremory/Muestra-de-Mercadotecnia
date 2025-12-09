-- ==========================================================
-- SCRIPT DE LÓGICA DE NEGOCIO (PL/SQL) - "MARKETING VIBE"
-- ==========================================================
-- Este script debe ejecutarse DESPUÉS de crear las tablas.
-- Contiene: Especificaciones y Cuerpos de Paquetes.
-- ==========================================================

SET SERVEROUTPUT ON;

-- ==========================================================
-- 1. PAQUETE DE EQUIPOS (PKG_EQUIPOS)
-- ==========================================================
-- Gestión del ABC de la tabla Equipos.

-- 1.1. ESPECIFICACIÓN
CREATE OR REPLACE PACKAGE pkg_equipos AS
    -- Alta
    PROCEDURE p_crear_equipo (
        p_nombre IN Equipos.Nombre%TYPE
    );
    -- Modificación
    PROCEDURE p_actualizar_equipo (
        p_id_equipo IN Equipos.IdEquipo%TYPE,
        p_nombre IN Equipos.Nombre%TYPE
    );
    -- Baja
    PROCEDURE p_eliminar_equipo (
        p_id_equipo IN Equipos.IdEquipo%TYPE
    );
    -- Consulta (Individual)
    FUNCTION f_obtener_equipo (
        p_id_equipo IN Equipos.IdEquipo%TYPE
    ) RETURN SYS_REFCURSOR;
    -- Consulta (Todos)
    FUNCTION f_obtener_todos_equipos
    RETURN SYS_REFCURSOR;
END pkg_equipos;
/

-- 1.2. CUERPO
CREATE OR REPLACE PACKAGE BODY pkg_equipos AS

    PROCEDURE p_crear_equipo (
        p_nombre IN Equipos.Nombre%TYPE
    ) AS
    BEGIN
        INSERT INTO Equipos (IdEquipo, Nombre)
        VALUES (equipos_seq.NEXTVAL, p_nombre);
        COMMIT;
    EXCEPTION
        WHEN OTHERS THEN
            ROLLBACK;
            RAISE_APPLICATION_ERROR(-20001, 'Error al crear el equipo: ' || SQLERRM);
    END p_crear_equipo;

    PROCEDURE p_actualizar_equipo (
        p_id_equipo IN Equipos.IdEquipo%TYPE,
        p_nombre IN Equipos.Nombre%TYPE
    ) AS
    BEGIN
        UPDATE Equipos
        SET Nombre = p_nombre
        WHERE IdEquipo = p_id_equipo;
        COMMIT;
    EXCEPTION
        WHEN OTHERS THEN
            ROLLBACK;
            RAISE_APPLICATION_ERROR(-20002, 'Error al actualizar el equipo: ' || SQLERRM);
    END p_actualizar_equipo;

    PROCEDURE p_eliminar_equipo (
        p_id_equipo IN Equipos.IdEquipo%TYPE
    ) AS
    BEGIN
        -- Idealmente validar dependencias antes de borrar
        DELETE FROM Equipos
        WHERE IdEquipo = p_id_equipo;
        COMMIT;
    EXCEPTION
        WHEN OTHERS THEN
            ROLLBACK;
            RAISE_APPLICATION_ERROR(-20003, 'Error al eliminar el equipo: ' || SQLERRM);
    END p_eliminar_equipo;

    FUNCTION f_obtener_equipo (
        p_id_equipo IN Equipos.IdEquipo%TYPE
    ) RETURN SYS_REFCURSOR AS
        v_cursor SYS_REFCURSOR;
    BEGIN
        OPEN v_cursor FOR
            SELECT IdEquipo, Nombre
            FROM Equipos
            WHERE IdEquipo = p_id_equipo;
        RETURN v_cursor;
    END f_obtener_equipo;

    FUNCTION f_obtener_todos_equipos
    RETURN SYS_REFCURSOR AS
        v_cursor SYS_REFCURSOR;
    BEGIN
        OPEN v_cursor FOR
            SELECT IdEquipo, Nombre
            FROM Equipos
            ORDER BY Nombre;
        RETURN v_cursor;
    END f_obtener_todos_equipos;

END pkg_equipos;
/


-- ==========================================================
-- 2. PAQUETE DE CATEGORÍAS (PKG_CATEGORIAS)
-- ==========================================================
-- Gestión del ABC de la tabla Categorías.

-- 2.1. ESPECIFICACIÓN
CREATE OR REPLACE PACKAGE pkg_categorias AS
    PROCEDURE p_crear_categoria (
        p_nombre IN Categorias.NombreCategoria%TYPE
    );
    PROCEDURE p_actualizar_categoria (
        p_id_categoria IN Categorias.IdCategoria%TYPE,
        p_nombre IN Categorias.NombreCategoria%TYPE
    );
    PROCEDURE p_eliminar_categoria (
        p_id_categoria IN Categorias.IdCategoria%TYPE
    );
    FUNCTION f_obtener_categoria (
        p_id_categoria IN Categorias.IdCategoria%TYPE
    ) RETURN SYS_REFCURSOR;
    FUNCTION f_obtener_todas_categorias
    RETURN SYS_REFCURSOR;
END pkg_categorias;
/

-- 2.2. CUERPO
CREATE OR REPLACE PACKAGE BODY pkg_categorias AS

    PROCEDURE p_crear_categoria (
        p_nombre IN Categorias.NombreCategoria%TYPE
    ) AS
    BEGIN
        INSERT INTO Categorias (IdCategoria, NombreCategoria)
        VALUES (categorias_seq.NEXTVAL, p_nombre);
        COMMIT;
    EXCEPTION
        WHEN OTHERS THEN
            ROLLBACK;
            RAISE_APPLICATION_ERROR(-20011, 'Error al crear la categoria: ' || SQLERRM);
    END p_crear_categoria;

    PROCEDURE p_actualizar_categoria (
        p_id_categoria IN Categorias.IdCategoria%TYPE,
        p_nombre IN Categorias.NombreCategoria%TYPE
    ) AS
    BEGIN
        UPDATE Categorias
        SET NombreCategoria = p_nombre
        WHERE IdCategoria = p_id_categoria;
        COMMIT;
    EXCEPTION
        WHEN OTHERS THEN
            ROLLBACK;
            RAISE_APPLICATION_ERROR(-20012, 'Error al actualizar la categoria: ' || SQLERRM);
    END p_actualizar_categoria;

    PROCEDURE p_eliminar_categoria (
        p_id_categoria IN Categorias.IdCategoria%TYPE
    ) AS
    BEGIN
        DELETE FROM Categorias
        WHERE IdCategoria = p_id_categoria;
        COMMIT;
    EXCEPTION
        WHEN OTHERS THEN
            ROLLBACK;
            RAISE_APPLICATION_ERROR(-20013, 'Error al eliminar la categoria: ' || SQLERRM);
    END p_eliminar_categoria;

    FUNCTION f_obtener_categoria (
        p_id_categoria IN Categorias.IdCategoria%TYPE
    ) RETURN SYS_REFCURSOR AS
        v_cursor SYS_REFCURSOR;
    BEGIN
        OPEN v_cursor FOR
            SELECT IdCategoria, NombreCategoria
            FROM Categorias
            WHERE IdCategoria = p_id_categoria;
        RETURN v_cursor;
    END f_obtener_categoria;

    FUNCTION f_obtener_todas_categorias
    RETURN SYS_REFCURSOR AS
        v_cursor SYS_REFCURSOR;
    BEGIN
        OPEN v_cursor FOR
            SELECT IdCategoria, NombreCategoria
            FROM Categorias
            ORDER BY NombreCategoria;
        RETURN v_cursor;
    END f_obtener_todas_categorias;

END pkg_categorias;
/


-- ==========================================================
-- 3. PAQUETE DE USUARIOS (PKG_USUARIOS)
-- ==========================================================
-- Gestión central de usuarios y login.

-- 3.1. ESPECIFICACIÓN
CREATE OR REPLACE PACKAGE pkg_usuarios AS
    PROCEDURE p_crear_usuario (
        p_correo IN Usuarios.Correo%TYPE,
        p_contrasena IN Usuarios.Contraseña%TYPE,
        p_nombre IN Usuarios.Nombre%TYPE,
        p_id_rol IN Usuarios.IdRol%TYPE,
        p_matricula IN Usuarios.Matricula%TYPE DEFAULT NULL,
        p_semestre IN Usuarios.Semestre%TYPE DEFAULT NULL,
        p_id_equipo IN Usuarios.IdEquipo%TYPE DEFAULT NULL
    );
    PROCEDURE p_actualizar_usuario (
        p_id_usuario IN Usuarios.IdUsuario%TYPE,
        p_correo IN Usuarios.Correo%TYPE,
        p_nombre IN Usuarios.Nombre%TYPE,
        p_id_rol IN Usuarios.IdRol%TYPE,
        p_matricula IN Usuarios.Matricula%TYPE,
        p_semestre IN Usuarios.Semestre%TYPE,
        p_id_equipo IN Usuarios.IdEquipo%TYPE
    );
    PROCEDURE p_eliminar_usuario (
        p_id_usuario IN Usuarios.IdUsuario%TYPE
    );
    FUNCTION f_obtener_usuario (
        p_id_usuario IN Usuarios.IdUsuario%TYPE
    ) RETURN SYS_REFCURSOR;
    -- Función vital para el Login
    FUNCTION f_obtener_usuario_por_correo (
        p_correo IN Usuarios.Correo%TYPE
    ) RETURN SYS_REFCURSOR;
    FUNCTION f_obtener_todos_usuarios
    RETURN SYS_REFCURSOR;
END pkg_usuarios;
/

-- 3.2. CUERPO
CREATE OR REPLACE PACKAGE BODY pkg_usuarios AS

    PROCEDURE p_crear_usuario (
        p_correo IN Usuarios.Correo%TYPE,
        p_contrasena IN Usuarios.Contraseña%TYPE,
        p_nombre IN Usuarios.Nombre%TYPE,
        p_id_rol IN Usuarios.IdRol%TYPE,
        p_matricula IN Usuarios.Matricula%TYPE DEFAULT NULL,
        p_semestre IN Usuarios.Semestre%TYPE DEFAULT NULL,
        p_id_equipo IN Usuarios.IdEquipo%TYPE DEFAULT NULL
    ) AS
    BEGIN
        INSERT INTO Usuarios (
            IdUsuario, Correo, Contraseña, Nombre, IdRol,
            Matricula, Semestre, IdEquipo
        )
        VALUES (
            usuarios_seq.NEXTVAL, p_correo, p_contrasena, p_nombre, p_id_rol,
            p_matricula, p_semestre, p_id_equipo
        );
        COMMIT;
    EXCEPTION
        WHEN OTHERS THEN
            ROLLBACK;
            RAISE_APPLICATION_ERROR(-20021, 'Error al crear el usuario: ' || SQLERRM);
    END p_crear_usuario;

    PROCEDURE p_actualizar_usuario (
        p_id_usuario IN Usuarios.IdUsuario%TYPE,
        p_correo IN Usuarios.Correo%TYPE,
        p_nombre IN Usuarios.Nombre%TYPE,
        p_id_rol IN Usuarios.IdRol%TYPE,
        p_matricula IN Usuarios.Matricula%TYPE,
        p_semestre IN Usuarios.Semestre%TYPE,
        p_id_equipo IN Usuarios.IdEquipo%TYPE
    ) AS
    BEGIN
        UPDATE Usuarios
        SET Correo = p_correo,
            Nombre = p_nombre,
            IdRol = p_id_rol,
            Matricula = p_matricula,
            Semestre = p_semestre,
            IdEquipo = p_id_equipo
        WHERE IdUsuario = p_id_usuario;
        COMMIT;
    EXCEPTION
        WHEN OTHERS THEN
            ROLLBACK;
            RAISE_APPLICATION_ERROR(-20022, 'Error al actualizar el usuario: ' || SQLERRM);
    END p_actualizar_usuario;

    PROCEDURE p_eliminar_usuario (
        p_id_usuario IN Usuarios.IdUsuario%TYPE
    ) AS
    BEGIN
        DELETE FROM Usuarios
        WHERE IdUsuario = p_id_usuario;
        COMMIT;
    EXCEPTION
        WHEN OTHERS THEN
            ROLLBACK;
            RAISE_APPLICATION_ERROR(-20023, 'Error al eliminar el usuario: ' || SQLERRM);
    END p_eliminar_usuario;

    FUNCTION f_obtener_usuario (
        p_id_usuario IN Usuarios.IdUsuario%TYPE
    ) RETURN SYS_REFCURSOR AS
        v_cursor SYS_REFCURSOR;
    BEGIN
        OPEN v_cursor FOR
            SELECT U.*, R.NombreRol
            FROM Usuarios U
            JOIN Roles R ON U.IdRol = R.IdRol
            WHERE U.IdUsuario = p_id_usuario;
        RETURN v_cursor;
    END f_obtener_usuario;

    FUNCTION f_obtener_usuario_por_correo (
        p_correo IN Usuarios.Correo%TYPE
    ) RETURN SYS_REFCURSOR AS
        v_cursor SYS_REFCURSOR;
    BEGIN
        OPEN v_cursor FOR
            SELECT U.*, R.NombreRol
            FROM Usuarios U
            JOIN Roles R ON U.IdRol = R.IdRol
            WHERE U.Correo = p_correo;
        RETURN v_cursor;
    END f_obtener_usuario_por_correo;

    FUNCTION f_obtener_todos_usuarios
    RETURN SYS_REFCURSOR AS
        v_cursor SYS_REFCURSOR;
    BEGIN
        OPEN v_cursor FOR
            SELECT U.IdUsuario, U.Nombre, U.Correo, U.Matricula, U.Semestre, R.NombreRol
            FROM Usuarios U
            JOIN Roles R ON U.IdRol = R.IdRol
            ORDER BY U.Nombre;
        RETURN v_cursor;
    END f_obtener_todos_usuarios;

END pkg_usuarios;
/


-- ==========================================================
-- 4. PAQUETE DE PROYECTOS (PKG_PROYECTOS)
-- ==========================================================
-- Gestión de la entidad principal.

-- 4.1. ESPECIFICACIÓN
CREATE OR REPLACE PACKAGE pkg_proyectos AS
    PROCEDURE p_crear_proyecto (
        p_id_equipo IN Proyectos.IdEquipo%TYPE,
        p_id_categoria IN Proyectos.IdCategoria%TYPE,
        p_nombre_proyecto IN Proyectos.NombreProyecto%TYPE
    );
    PROCEDURE p_actualizar_proyecto (
        p_id_proyecto IN Proyectos.IdProyecto%TYPE,
        p_nombre_proyecto IN Proyectos.NombreProyecto%TYPE,
        p_ruta_pdf IN Proyectos.RutaArchivoPDF%TYPE,
        p_ruta_adicionales IN Proyectos.RutaArchivosAdicionales%TYPE
    );
    PROCEDURE p_eliminar_proyecto (
        p_id_proyecto IN Proyectos.IdProyecto%TYPE
    );
    PROCEDURE p_actualizar_estado_proyecto (
        p_id_proyecto IN Proyectos.IdProyecto%TYPE,
        p_nuevo_estado IN Proyectos.Estado%TYPE
    );
    PROCEDURE p_asignar_calificacion_final (
        p_id_proyecto IN Proyectos.IdProyecto%TYPE,
        p_calificacion IN Proyectos.CalificacionFinal%TYPE,
        p_reconocimiento IN Proyectos.Reconocimiento%TYPE
    );
    FUNCTION f_obtener_proyecto_detalle (
        p_id_proyecto IN Proyectos.IdProyecto%TYPE
    ) RETURN SYS_REFCURSOR;
    FUNCTION f_obtener_todos_proyectos
    RETURN SYS_REFCURSOR;
END pkg_proyectos;
/

-- 4.2. CUERPO
CREATE OR REPLACE PACKAGE BODY pkg_proyectos AS

    PROCEDURE p_crear_proyecto (
        p_id_equipo IN Proyectos.IdEquipo%TYPE,
        p_id_categoria IN Proyectos.IdCategoria%TYPE,
        p_nombre_proyecto IN Proyectos.NombreProyecto%TYPE
    ) AS
    BEGIN
        INSERT INTO Proyectos (
            IdProyecto, IdEquipo, IdCategoria, NombreProyecto, Estado
        )
        VALUES (
            proyectos_seq.NEXTVAL, p_id_equipo, p_id_categoria, p_nombre_proyecto, 'Pendiente'
        );
        COMMIT;
    EXCEPTION
        WHEN OTHERS THEN
            ROLLBACK;
            RAISE_APPLICATION_ERROR(-20031, 'Error al crear el proyecto: ' || SQLERRM);
    END p_crear_proyecto;

    PROCEDURE p_actualizar_proyecto (
        p_id_proyecto IN Proyectos.IdProyecto%TYPE,
        p_nombre_proyecto IN Proyectos.NombreProyecto%TYPE,
        p_ruta_pdf IN Proyectos.RutaArchivoPDF%TYPE,
        p_ruta_adicionales IN Proyectos.RutaArchivosAdicionales%TYPE
    ) AS
    BEGIN
        UPDATE Proyectos
        SET NombreProyecto = p_nombre_proyecto,
            RutaArchivoPDF = p_ruta_pdf,
            RutaArchivosAdicionales = p_ruta_adicionales
        WHERE IdProyecto = p_id_proyecto;
        COMMIT;
    EXCEPTION
        WHEN OTHERS THEN
            ROLLBACK;
            RAISE_APPLICATION_ERROR(-20032, 'Error al actualizar el proyecto: ' || SQLERRM);
    END p_actualizar_proyecto;

    PROCEDURE p_eliminar_proyecto (
        p_id_proyecto IN Proyectos.IdProyecto%TYPE
    ) AS
    BEGIN
        DELETE FROM Proyectos
        WHERE IdProyecto = p_id_proyecto;
        COMMIT;
    EXCEPTION
        WHEN OTHERS THEN
            ROLLBACK;
            RAISE_APPLICATION_ERROR(-20033, 'Error al eliminar el proyecto: ' || SQLERRM);
    END p_eliminar_proyecto;

    PROCEDURE p_actualizar_estado_proyecto (
        p_id_proyecto IN Proyectos.IdProyecto%TYPE,
        p_nuevo_estado IN Proyectos.Estado%TYPE
    ) AS
    BEGIN
        UPDATE Proyectos
        SET Estado = p_nuevo_estado
        WHERE IdProyecto = p_id_proyecto;
        COMMIT;
    EXCEPTION
        WHEN OTHERS THEN
            ROLLBACK;
            RAISE_APPLICATION_ERROR(-20034, 'Error al actualizar el estado: ' || SQLERRM);
    END p_actualizar_estado_proyecto;

    PROCEDURE p_asignar_calificacion_final (
        p_id_proyecto IN Proyectos.IdProyecto%TYPE,
        p_calificacion IN Proyectos.CalificacionFinal%TYPE,
        p_reconocimiento IN Proyectos.Reconocimiento%TYPE
    ) AS
    BEGIN
        UPDATE Proyectos
        SET CalificacionFinal = p_calificacion,
            Reconocimiento = p_reconocimiento,
            Estado = 'Evaluado'
        WHERE IdProyecto = p_id_proyecto;
        COMMIT;
    EXCEPTION
        WHEN OTHERS THEN
            ROLLBACK;
            RAISE_APPLICATION_ERROR(-20035, 'Error al asignar calificacion: ' || SQLERRM);
    END p_asignar_calificacion_final;

    FUNCTION f_obtener_proyecto_detalle (
        p_id_proyecto IN Proyectos.IdProyecto%TYPE
    ) RETURN SYS_REFCURSOR AS
        v_cursor SYS_REFCURSOR;
    BEGIN
        OPEN v_cursor FOR
            SELECT
                P.*,
                E.Nombre AS NombreEquipo,
                C.NombreCategoria AS NombreCategoria
            FROM Proyectos P
            JOIN Equipos E ON P.IdEquipo = E.IdEquipo
            JOIN Categorias C ON P.IdCategoria = C.IdCategoria
            WHERE P.IdProyecto = p_id_proyecto;
        RETURN v_cursor;
    END f_obtener_proyecto_detalle;

    FUNCTION f_obtener_todos_proyectos
    RETURN SYS_REFCURSOR AS
        v_cursor SYS_REFCURSOR;
    BEGIN
        OPEN v_cursor FOR
            SELECT
                P.IdProyecto, P.NombreProyecto, P.Estado, P.CalificacionFinal,
                E.Nombre AS NombreEquipo,
                C.NombreCategoria AS NombreCategoria
            FROM Proyectos P
            JOIN Equipos E ON P.IdEquipo = E.IdEquipo
            JOIN Categorias C ON P.IdCategoria = C.IdCategoria
            ORDER BY E.Nombre, P.NombreProyecto;
        RETURN v_cursor;
    END f_obtener_todos_proyectos;

END pkg_proyectos;
/


-- ==========================================================
-- 5. PAQUETE DE ASIGNACIONES (PKG_ASIGNACIONES)
-- ==========================================================
-- Gestión de Jueces y Evaluaciones.

-- 5.1. ESPECIFICACIÓN
CREATE OR REPLACE PACKAGE pkg_asignaciones AS
    PROCEDURE p_crear_asignacion (
        p_id_proyecto IN Asignaciones.IdProyecto%TYPE,
        p_id_usuario IN Asignaciones.IdUsuario_Evaluador%TYPE
    );
    PROCEDURE p_eliminar_asignacion (
        p_id_asignacion IN Asignaciones.IdAsignacion%TYPE
    );
    -- Para el Dashboard del Juez
    FUNCTION f_obtener_proyectos_por_juez (
        p_id_usuario IN Asignaciones.IdUsuario_Evaluador%TYPE
    ) RETURN SYS_REFCURSOR;
    -- Para ver quién evalúa qué
    FUNCTION f_obtener_jueces_por_proyecto (
        p_id_proyecto IN Asignaciones.IdProyecto%TYPE
    ) RETURN SYS_REFCURSOR;
END pkg_asignaciones;
/

-- 5.2. CUERPO
CREATE OR REPLACE PACKAGE BODY pkg_asignaciones AS

    PROCEDURE p_crear_asignacion (
        p_id_proyecto IN Asignaciones.IdProyecto%TYPE,
        p_id_usuario IN Asignaciones.IdUsuario_Evaluador%TYPE
    ) AS
    BEGIN
        -- Insertamos con estado pendiente
        INSERT INTO Asignaciones (IdAsignacion, IdProyecto, IdUsuario_Evaluador, Estado)
        VALUES (asignaciones_seq.NEXTVAL, p_id_proyecto, p_id_usuario, 'Pendiente');
        COMMIT;
    EXCEPTION
        WHEN DUP_VAL_ON_INDEX THEN
            ROLLBACK;
            RAISE_APPLICATION_ERROR(-20041, 'El juez ya esta asignado a este proyecto.');
        WHEN OTHERS THEN
            ROLLBACK;
            RAISE_APPLICATION_ERROR(-20042, 'Error al asignar juez: ' || SQLERRM);
    END p_crear_asignacion;

    PROCEDURE p_eliminar_asignacion (
        p_id_asignacion IN Asignaciones.IdAsignacion%TYPE
    ) AS
    BEGIN
        DELETE FROM Asignaciones
        WHERE IdAsignacion = p_id_asignacion;
        COMMIT;
    EXCEPTION
        WHEN OTHERS THEN
            ROLLBACK;
            RAISE_APPLICATION_ERROR(-20043, 'Error al eliminar asignacion: ' || SQLERRM);
    END p_eliminar_asignacion;

    FUNCTION f_obtener_proyectos_por_juez (
        p_id_usuario IN Asignaciones.IdUsuario_Evaluador%TYPE
    ) RETURN SYS_REFCURSOR AS
        v_cursor SYS_REFCURSOR;
    BEGIN
        OPEN v_cursor FOR
            SELECT
                A.IdAsignacion,
                A.Estado AS EstadoAsignacion,
                P.NombreProyecto,
                P.RutaArchivoPDF,
                C.NombreCategoria,
                E.Nombre AS NombreEquipo
            FROM Asignaciones A
            JOIN Proyectos P ON A.IdProyecto = P.IdProyecto
            JOIN Categorias C ON P.IdCategoria = C.IdCategoria
            JOIN Equipos E ON P.IdEquipo = E.IdEquipo
            WHERE A.IdUsuario_Evaluador = p_id_usuario
            ORDER BY A.Estado, P.NombreProyecto;
        RETURN v_cursor;
    END f_obtener_proyectos_por_juez;

    FUNCTION f_obtener_jueces_por_proyecto (
        p_id_proyecto IN Asignaciones.IdProyecto%TYPE
    ) RETURN SYS_REFCURSOR AS
        v_cursor SYS_REFCURSOR;
    BEGIN
        OPEN v_cursor FOR
            SELECT
                A.IdAsignacion,
                A.Estado,
                U.Nombre AS NombreJuez,
                U.Correo
            FROM Asignaciones A
            JOIN Usuarios U ON A.IdUsuario_Evaluador = U.IdUsuario
            WHERE A.IdProyecto = p_id_proyecto;
        RETURN v_cursor;
    END f_obtener_jueces_por_proyecto;

END pkg_asignaciones;
/


-- ==========================================================
-- 6. PAQUETE DE EVALUACIONES (PKG_EVALUACIONES)
-- ==========================================================
-- Lógica transaccional de calificaciones (MERGE).

-- 6.1. ESPECIFICACIÓN
CREATE OR REPLACE PACKAGE pkg_evaluaciones AS
    PROCEDURE p_guardar_calificacion (
        p_id_asignacion IN Evaluaciones.IdAsignacion%TYPE,
        p_id_criterio IN Evaluaciones.IdCriterio%TYPE,
        p_puntaje IN Evaluaciones.PuntajeObtenido%TYPE,
        p_comentarios IN Evaluaciones.Comentarios%TYPE DEFAULT NULL
    );
    FUNCTION f_obtener_eval_asignacion (
        p_id_asignacion IN Evaluaciones.IdAsignacion%TYPE
    ) RETURN SYS_REFCURSOR;
    FUNCTION f_calcular_total_asignacion (
        p_id_asignacion IN Evaluaciones.IdAsignacion%TYPE
    ) RETURN NUMBER;
END pkg_evaluaciones;
/

-- 6.2. CUERPO
CREATE OR REPLACE PACKAGE BODY pkg_evaluaciones AS

    PROCEDURE p_guardar_calificacion (
        p_id_asignacion IN Evaluaciones.IdAsignacion%TYPE,
        p_id_criterio IN Evaluaciones.IdCriterio%TYPE,
        p_puntaje IN Evaluaciones.PuntajeObtenido%TYPE,
        p_comentarios IN Evaluaciones.Comentarios%TYPE DEFAULT NULL
    ) AS
    BEGIN
        -- MERGE para Insertar o Actualizar
        MERGE INTO Evaluaciones E
        USING DUAL ON (E.IdAsignacion = p_id_asignacion AND E.IdCriterio = p_id_criterio)
        WHEN MATCHED THEN
            UPDATE SET PuntajeObtenido = p_puntaje, Comentarios = p_comentarios
        WHEN NOT MATCHED THEN
            INSERT (IdEvaluacion, IdAsignacion, IdCriterio, PuntajeObtenido, Comentarios)
            VALUES (evaluaciones_seq.NEXTVAL, p_id_asignacion, p_id_criterio, p_puntaje, p_comentarios);
        COMMIT;
    EXCEPTION
        WHEN OTHERS THEN
            ROLLBACK;
            RAISE_APPLICATION_ERROR(-20051, 'Error al guardar calificacion: ' || SQLERRM);
    END p_guardar_calificacion;

    FUNCTION f_obtener_eval_asignacion (
        p_id_asignacion IN Evaluaciones.IdAsignacion%TYPE
    ) RETURN SYS_REFCURSOR AS
        v_cursor SYS_REFCURSOR;
    BEGIN
        OPEN v_cursor FOR
            SELECT
                E.IdEvaluacion,
                C.NombreCriterio,
                C.PesoPorcentual,
                E.PuntajeObtenido,
                E.Comentarios
            FROM Criterios C
            LEFT JOIN Evaluaciones E
                ON C.IdCriterio = E.IdCriterio AND E.IdAsignacion = p_id_asignacion
            -- Nota: Aquí podrías filtrar por C.IdCategoria si tuvieras esa info a mano
            -- para solo mostrar criterios relevantes, pero por ahora mostramos lo que hay.
            ORDER BY C.IdCriterio;
        RETURN v_cursor;
    END f_obtener_eval_asignacion;

    FUNCTION f_calcular_total_asignacion (
        p_id_asignacion IN Evaluaciones.IdAsignacion%TYPE
    ) RETURN NUMBER AS
        v_total NUMBER := 0;
    BEGIN
        SELECT NVL(SUM(PuntajeObtenido), 0)
        INTO v_total
        FROM Evaluaciones
        WHERE IdAsignacion = p_id_asignacion;
        RETURN v_total;
    END f_calcular_total_asignacion;

END pkg_evaluaciones;
/


-- ==========================================================
-- 7. PAQUETE DE REPORTES AVANZADOS (PKG_REPORTES)
-- ==========================================================
-- Reportes de División, Multitabla y Cursores Explícitos.

-- 7.1. ESPECIFICACIÓN
CREATE OR REPLACE PACKAGE pkg_reportes AS
    -- Reporte de División: Jueces que han terminado TODO
    FUNCTION f_jueces_cumplidos
    RETURN SYS_REFCURSOR;

    -- Reporte Multitabla (Ranking con Window Function)
    FUNCTION f_ranking_ganadores
    RETURN SYS_REFCURSOR;

    -- Cursor Explícito (Salida a consola)
    PROCEDURE p_imprimir_reporte_consola;
END pkg_reportes;
/

-- 7.2. CUERPO
CREATE OR REPLACE PACKAGE BODY pkg_reportes AS

    FUNCTION f_jueces_cumplidos RETURN SYS_REFCURSOR AS
        v_cursor SYS_REFCURSOR;
    BEGIN
        -- Lógica de División: Jueces tal que NO EXISTE una asignación pendiente.
        OPEN v_cursor FOR
            SELECT U.IdUsuario, U.Nombre, U.Correo
            FROM Usuarios U
            WHERE U.IdRol = 5 -- Solo Evaluadores
            AND EXISTS (SELECT 1 FROM Asignaciones A WHERE A.IdUsuario_Evaluador = U.IdUsuario)
            AND NOT EXISTS (
                SELECT 1
                FROM Asignaciones A_Pendiente
                WHERE A_Pendiente.IdUsuario_Evaluador = U.IdUsuario
                AND A_Pendiente.Estado != 'Evaluado'
            );
        RETURN v_cursor;
    END f_jueces_cumplidos;

    FUNCTION f_ranking_ganadores RETURN SYS_REFCURSOR AS
        v_cursor SYS_REFCURSOR;
    BEGIN
        OPEN v_cursor FOR
            SELECT
                RANK() OVER (PARTITION BY C.NombreCategoria ORDER BY P.CalificacionFinal DESC) as Posicion,
                C.NombreCategoria,
                P.NombreProyecto,
                E.Nombre as NombreEquipo,
                P.CalificacionFinal
            FROM Proyectos P
            JOIN Categorias C ON P.IdCategoria = C.IdCategoria
            JOIN Equipos E ON P.IdEquipo = E.IdEquipo
            WHERE P.Estado = 'Evaluado'
            ORDER BY C.NombreCategoria, P.CalificacionFinal DESC;
        RETURN v_cursor;
    END f_ranking_ganadores;

    PROCEDURE p_imprimir_reporte_consola IS
        -- 1. Declaración de Cursor Explícito (Tipo 1)
        CURSOR c_categorias IS
            SELECT IdCategoria, NombreCategoria FROM Categorias;
        -- 2. Declaración de Cursor Parametrizado (Tipo 2)
        CURSOR c_proyectos_por_cat(p_id_cat NUMBER) IS
            SELECT NombreProyecto, Estado
            FROM Proyectos
            WHERE IdCategoria = p_id_cat;

        v_cat_nombre VARCHAR2(150);
        v_cat_id NUMBER;
        -- Variables para el fetch (aunque usaremos FOR LOOP para el segundo)
    BEGIN
        DBMS_OUTPUT.PUT_LINE('=== REPORTE DETALLADO POR CONSOLA ===');

        -- Uso de ciclo LOOP para Cursor Explícito básico
        OPEN c_categorias;
        LOOP
            FETCH c_categorias INTO v_cat_id, v_cat_nombre;
            EXIT WHEN c_categorias%NOTFOUND;

            DBMS_OUTPUT.PUT_LINE('----------------------------------------');
            DBMS_OUTPUT.PUT_LINE('CATEGORÍA: ' || v_cat_nombre);

            -- Uso de ciclo FOR para Cursor Parametrizado (Más elegante)
            FOR r_proy IN c_proyectos_por_cat(v_cat_id) LOOP
                DBMS_OUTPUT.PUT_LINE('   > Proyecto: ' || r_proy.NombreProyecto || ' [' || r_proy.Estado || ']');
            END LOOP;
        END LOOP;
        CLOSE c_categorias;
        DBMS_OUTPUT.PUT_LINE('========================================');
    END p_imprimir_reporte_consola;

END pkg_reportes;
/

-- ==========================================================
-- FIN DEL SCRIPT
-- ==========================================================