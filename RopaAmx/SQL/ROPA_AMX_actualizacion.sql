-- =====================================================
-- Para tu base ROPA_AMX YA EXISTENTE: agrega el rol de usuario
-- =====================================================
USE ROPA_AMX;
GO

IF COL_LENGTH('dbo.Usuarios', 'Rol') IS NULL
    ALTER TABLE dbo.Usuarios ADD Rol VARCHAR(20) NOT NULL CONSTRAINT DF_Usuarios_Rol DEFAULT 'Lector';
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_Usuarios_Correo')
    CREATE UNIQUE INDEX UX_Usuarios_Correo
        ON dbo.Usuarios(CorreoElectronico) WHERE CorreoElectronico IS NOT NULL;
GO
