-- ==========================================================
-- SCRIPT COMPLETO DE BASE DE DATOS (VERSIÓN 2.1 - LIMPIA)
-- MUESTRA MERCADOLÓGICA "MARKETING VIBE"
-- ==========================================================

-- ==========================================================
-- 1. BORRADO SEGURO DE TABLAS (EN ORDEN DE DEPENDENCIAS)
-- ==========================================================
BEGIN
   EXECUTE IMMEDIATE 'DROP TABLE Evaluaciones';
EXCEPTION
   WHEN OTHERS THEN
      IF SQLCODE != -942 THEN
         RAISE;
      END IF;
END;
/
BEGIN
   EXECUTE IMMEDIATE 'DROP TABLE Criterios';
EXCEPTION
   WHEN OTHERS THEN
      IF SQLCODE != -942 THEN
         RAISE;
      END IF;
END;
/
BEGIN
   EXECUTE IMMEDIATE 'DROP TABLE Asignaciones';
EXCEPTION
   WHEN OTHERS THEN
      IF SQLCODE != -942 THEN
         RAISE;
      END IF;
END;
/
BEGIN
   EXECUTE IMMEDIATE 'DROP TABLE Proyectos';
EXCEPTION
   WHEN OTHERS THEN
      IF SQLCODE != -942 THEN
         RAISE;
      END IF;
END;
/
BEGIN
   EXECUTE IMMEDIATE 'DROP TABLE Miembros';
EXCEPTION
   WHEN OTHERS THEN
      IF SQLCODE != -942 THEN
         RAISE;
      END IF;
END;
/
BEGIN
   EXECUTE IMMEDIATE 'DROP TABLE Usuarios';
EXCEPTION
   WHEN OTHERS THEN
      IF SQLCODE != -942 THEN
         RAISE;
      END IF;
END;
/
BEGIN
   EXECUTE IMMEDIATE 'DROP TABLE Galeria';
EXCEPTION
   WHEN OTHERS THEN
      IF SQLCODE != -942 THEN
         RAISE;
      END IF;
END;
/
BEGIN
   EXECUTE IMMEDIATE 'DROP TABLE Equipos';
EXCEPTION
   WHEN OTHERS THEN
      IF SQLCODE != -942 THEN
         RAISE;
      END IF;
END;
/
BEGIN
   EXECUTE IMMEDIATE 'DROP TABLE Categorias';
EXCEPTION
   WHEN OTHERS THEN
      IF SQLCODE != -942 THEN
         RAISE;
      END IF;
END;
/
BEGIN
   EXECUTE IMMEDIATE 'DROP TABLE Roles';
EXCEPTION
   WHEN OTHERS THEN
      IF SQLCODE != -942 THEN
         RAISE;
      END IF;
END;
/

-- ==========================================================
-- 2. BORRADO SEGURO DE SECUENCIAS
-- ==========================================================
BEGIN
   EXECUTE IMMEDIATE 'DROP SEQUENCE roles_seq';
EXCEPTION
   WHEN OTHERS THEN
      IF SQLCODE != -2289 THEN
         RAISE;
      END IF;
END;
/
BEGIN
   EXECUTE IMMEDIATE 'DROP SEQUENCE equipos_seq';
EXCEPTION
   WHEN OTHERS THEN
      IF SQLCODE != -2289 THEN
         RAISE;
      END IF;
END;
/
BEGIN
   EXECUTE IMMEDIATE 'DROP SEQUENCE usuarios_seq';
EXCEPTION
   WHEN OTHERS THEN
      IF SQLCODE != -2289 THEN
         RAISE;
      END IF;
END;
/
BEGIN
   EXECUTE IMMEDIATE 'DROP SEQUENCE miembros_seq';
EXCEPTION
   WHEN OTHERS THEN
      IF SQLCODE != -2289 THEN
         RAISE;
      END IF;
END;
/
BEGIN
   EXECUTE IMMEDIATE 'DROP SEQUENCE categorias_seq';
EXCEPTION
   WHEN OTHERS THEN
      IF SQLCODE != -2289 THEN
         RAISE;
      END IF;
END;
/
BEGIN
   EXECUTE IMMEDIATE 'DROP SEQUENCE proyectos_seq';
EXCEPTION
   WHEN OTHERS THEN
      IF SQLCODE != -2289 THEN
         RAISE;
      END IF;
END;
/
BEGIN
   EXECUTE IMMEDIATE 'DROP SEQUENCE asignaciones_seq';
EXCEPTION
   WHEN OTHERS THEN
      IF SQLCODE != -2289 THEN
         RAISE;
      END IF;
END;
/
BEGIN
   EXECUTE IMMEDIATE 'DROP SEQUENCE criterios_seq';
EXCEPTION
   WHEN OTHERS THEN
      IF SQLCODE != -2289 THEN
         RAISE;
      END IF;
END;
/
BEGIN
   EXECUTE IMMEDIATE 'DROP SEQUENCE evaluaciones_seq';
EXCEPTION
   WHEN OTHERS THEN
      IF SQLCODE != -2289 THEN
         RAISE;
      END IF;
END;
/
BEGIN
   EXECUTE IMMEDIATE 'DROP SEQUENCE galeria_seq';
EXCEPTION
   WHEN OTHERS THEN
      IF SQLCODE != -2289 THEN
         RAISE;
      END IF;
END;
/

-- ==========================================================
-- 3. CREACIÓN DE TABLAS
-- ==========================================================

CREATE TABLE Roles (
    IdRol NUMBER(10) PRIMARY KEY,
    NombreRol VARCHAR2(50 CHAR) NOT NULL UNIQUE
);

CREATE TABLE Equipos (
    IdEquipo NUMBER(10) PRIMARY KEY,
    Nombre VARCHAR2(100 CHAR) NOT NULL
);

CREATE TABLE Categorias (
    IdCategoria NUMBER(10) PRIMARY KEY,
    NombreCategoria VARCHAR2(150 CHAR) NOT NULL UNIQUE
);

CREATE TABLE Galeria (
    IdGaleria NUMBER(10) PRIMARY KEY,
    RutaArchivo VARCHAR2(500 CHAR) NOT NULL,
    Descripcion VARCHAR2(500 CHAR),
    Tipo VARCHAR2(10 CHAR) NOT NULL
);

CREATE TABLE Usuarios (
    IdUsuario NUMBER(10) PRIMARY KEY,
    Correo VARCHAR2(100 CHAR) NOT NULL UNIQUE,
    Contraseña VARCHAR2(255 CHAR) NOT NULL,
    Nombre VARCHAR2(150 CHAR) NOT NULL,
    Matricula VARCHAR2(20 CHAR),
    Semestre NUMBER(2),
    IdEquipo NUMBER(10),
    IdRol NUMBER(10) NOT NULL,
    CONSTRAINT fk_usuario_equipo FOREIGN KEY (IdEquipo) REFERENCES Equipos(IdEquipo),
    CONSTRAINT fk_usuario_rol FOREIGN KEY (IdRol) REFERENCES Roles(IdRol)
);

CREATE TABLE Miembros (
    IdMiembro NUMBER(10) PRIMARY KEY,
    Nombre VARCHAR2(150 CHAR) NOT NULL,
    Matricula VARCHAR2(20 CHAR) NOT NULL,
    IdEquipo NUMBER(10) NOT NULL,
    CONSTRAINT fk_miembro_equipo FOREIGN KEY (IdEquipo) REFERENCES Equipos(IdEquipo)
);

CREATE TABLE Proyectos (
    IdProyecto NUMBER(10) PRIMARY KEY,
    IdEquipo NUMBER(10) NOT NULL,
    IdCategoria NUMBER(10) NOT NULL,
    NombreProyecto VARCHAR2(255 CHAR) NOT NULL,
    Estado VARCHAR2(50 CHAR) DEFAULT 'Pendiente',
    RutaArchivoPDF VARCHAR2(500 CHAR),
    RutaArchivosAdicionales VARCHAR2(1000 CHAR),
    CalificacionFinal NUMBER(5, 2),
    Reconocimiento VARCHAR2(255 CHAR),
    CONSTRAINT fk_proyecto_equipo FOREIGN KEY (IdEquipo) REFERENCES Equipos(IdEquipo),
    CONSTRAINT fk_proyecto_categoria FOREIGN KEY (IdCategoria) REFERENCES Categorias(IdCategoria)
);

CREATE TABLE Asignaciones (
    IdAsignacion NUMBER(10) PRIMARY KEY,
    IdProyecto NUMBER(10) NOT NULL,
    IdUsuario_Evaluador NUMBER(10) NOT NULL,
    Estado VARCHAR2(50 CHAR) DEFAULT 'Pendiente',
    CONSTRAINT fk_asig_proyecto FOREIGN KEY (IdProyecto) REFERENCES Proyectos(IdProyecto),
    CONSTRAINT fk_asig_usuario FOREIGN KEY (IdUsuario_Evaluador) REFERENCES Usuarios(IdUsuario)
);

CREATE TABLE Criterios (
    IdCriterio NUMBER(10) PRIMARY KEY,
    IdCategoria NUMBER(10) NOT NULL,
    RolEvaluador VARCHAR2(50 CHAR) NOT NULL,
    NombreCriterio VARCHAR2(255 CHAR) NOT NULL,
    Materia VARCHAR2(255 CHAR),
    PesoPorcentual NUMBER(5, 2),
    CONSTRAINT fk_criterio_categoria FOREIGN KEY (IdCategoria) REFERENCES Categorias(IdCategoria)
);

CREATE TABLE Evaluaciones (
    IdEvaluacion NUMBER(10) PRIMARY KEY,
    IdAsignacion NUMBER(10) NOT NULL,
    IdCriterio NUMBER(10) NOT NULL,
    PuntajeObtenido NUMBER(5, 2) NOT NULL,
    Comentarios VARCHAR2(2000 CHAR),
    CONSTRAINT fk_eval_asignacion FOREIGN KEY (IdAsignacion) REFERENCES Asignaciones(IdAsignacion),
    CONSTRAINT fk_eval_criterio FOREIGN KEY (IdCriterio) REFERENCES Criterios(IdCriterio)
);


-- ==========================================================
-- 4. CREACIÓN DE SECUENCIAS
-- ==========================================================
CREATE SEQUENCE roles_seq START WITH 1 INCREMENT BY 1;
CREATE SEQUENCE equipos_seq START WITH 1 INCREMENT BY 1;
CREATE SEQUENCE usuarios_seq START WITH 1 INCREMENT BY 1;
CREATE SEQUENCE miembros_seq START WITH 1 INCREMENT BY 1;
CREATE SEQUENCE categorias_seq START WITH 1 INCREMENT BY 1;
CREATE SEQUENCE proyectos_seq START WITH 1 INCREMENT BY 1;
CREATE SEQUENCE asignaciones_seq START WITH 1 INCREMENT BY 1;
CREATE SEQUENCE criterios_seq START WITH 1 INCREMENT BY 1;
CREATE SEQUENCE evaluaciones_seq START WITH 1 INCREMENT BY 1;
CREATE SEQUENCE galeria_seq START WITH 1 INCREMENT BY 1;


-- ==========================================================
-- 5. DATOS INICIALES (NECESARIOS PARA EL SISTEMA)
-- ==========================================================

INSERT INTO Roles (IdRol, NombreRol) VALUES (1, 'Administrador');
INSERT INTO Roles (IdRol, NombreRol) VALUES (2, 'Staff');
INSERT INTO Roles (IdRol, NombreRol) VALUES (3, 'Docente');
INSERT INTO Roles (IdRol, NombreRol) VALUES (4, 'Equipo');
INSERT INTO Roles (IdRol, NombreRol) VALUES (5, 'Evaluador');

INSERT INTO Categorias (IdCategoria, NombreCategoria) VALUES (categorias_seq.NEXTVAL, 'Retail Revolution');
INSERT INTO Categorias (IdCategoria, NombreCategoria) VALUES (categorias_seq.NEXTVAL, 'Fresh Creations');

SELECT U.Nombre
FROM Usuarios U
WHERE U.IdRol = 5 
AND NOT EXISTS (
    SELECT P.IdProyecto
    FROM Proyectos P
    WHERE NOT EXISTS (
        SELECT A.IdAsignacion
        FROM Asignaciones A
        WHERE A.IdUsuario_Evaluador = U.IdUsuario
        AND A.IdProyecto = P.IdProyecto
    )
);

SELECT P.NombreProyecto
FROM Proyectos P
WHERE (
    SELECT COUNT(DISTINCT E.IdCriterio)
    FROM Evaluaciones E
    JOIN Asignaciones A ON E.IdAsignacion = A.IdAsignacion
    WHERE A.IdProyecto = P.IdProyecto
) = (
    SELECT COUNT(*)
    FROM Criterios C
    WHERE C.IdCategoria = P.IdCategoria
);

P.NombreProyecto, 
    E.Nombre AS Nombre_Equipo, 
    C.NombreCategoria, 
    P.Estado,
    P.CalificacionFinal
FROM Proyectos P
JOIN Equipos E ON P.IdEquipo = E.IdEquipo
JOIN Categorias C ON P.IdCategoria = C.IdCategoria
ORDER BY P.NombreProyecto;

SELECT 
    U.Nombre AS Evaluador,
    P.NombreProyecto,
    C.NombreCriterio,
    EV.PuntajeObtenido,
    EV.Comentarios
FROM Evaluaciones EV
JOIN Asignaciones A ON EV.IdAsignacion = A.IdAsignacion
JOIN Usuarios U ON A.IdUsuario_Evaluador = U.IdUsuario
JOIN Proyectos P ON A.IdProyecto = P.IdProyecto
JOIN Criterios C ON EV.IdCriterio = C.IdCriterio
ORDER BY P.NombreProyecto;
-- ==========================================================
-- 6. CONFIRMAR LOS CAMBIOS
-- ==========================================================
COMMIT;|