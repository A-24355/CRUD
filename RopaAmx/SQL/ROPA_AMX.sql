-- =====================================================
-- Base de datos: ROPA_AMX  (instalación desde cero)
-- En un hosting normalmente ya te dan la base creada:
-- en ese caso borra las 4 líneas de CREATE DATABASE / USE.
-- =====================================================
CREATE DATABASE ROPA_AMX;
GO
USE ROPA_AMX;
GO

CREATE TABLE dbo.Categorias (
    Id     INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    Nombre VARCHAR(50) NOT NULL,        -- Hombre, Mujer, Niño
    Slug   VARCHAR(50) NOT NULL UNIQUE  -- hombre, mujer, ninos (para la URL)
);
GO

CREATE TABLE dbo.Productos (
    Id            UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
    Nombre        VARCHAR(100)  NOT NULL,
    Descripcion   VARCHAR(250)  NULL,
    Precio        DECIMAL(10,2) NOT NULL,
    PrecioRebaja  DECIMAL(10,2) NULL,
    Tipo          VARCHAR(30)   NULL,
    Existencia    INT           NOT NULL DEFAULT 0,
    EsMasVendido  BIT           NOT NULL DEFAULT 0,
    ImagenUrl     VARCHAR(250)  NULL,
    IdCategoria   INT           NOT NULL,
    FechaRegistro DATETIME      NULL DEFAULT GETDATE(),
    CONSTRAINT FK_Productos_Categorias
        FOREIGN KEY (IdCategoria) REFERENCES dbo.Categorias(Id)
);
GO

CREATE TABLE dbo.Usuarios (
    Id                UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
    NombreCompleto    VARCHAR(120) NULL,
    CorreoElectronico VARCHAR(150) NULL,
    Password          VARCHAR(250) NULL,            -- hash PBKDF2, no texto plano
    Rol               VARCHAR(20)  NOT NULL DEFAULT 'Lector',  -- 'Administrador' o 'Lector'
    Activo            BIT NULL DEFAULT 1,
    FechaRegistro     DATETIME NULL DEFAULT GETDATE()
);
GO

CREATE UNIQUE INDEX UX_Usuarios_Correo
    ON dbo.Usuarios(CorreoElectronico) WHERE CorreoElectronico IS NOT NULL;
GO

-- Categorías base
INSERT INTO dbo.Categorias (Nombre, Slug) VALUES ('Hombre','hombre'), ('Mujer','mujer'), ('Niño','ninos');
GO
-- Los usuarios admin@amxk.mx y lector@amxk.mx los crea la aplicación al iniciar (con contraseña cifrada).
