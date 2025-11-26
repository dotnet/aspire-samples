-- SQL Server init script

-- Create the AddressBook database
IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = N'AddressBook')
BEGIN
  CREATE DATABASE AddressBook;
END;
GO

USE AddressBook;
GO

-- Create the Contacts table
IF OBJECT_ID(N'Contacts', N'U') IS NULL
BEGIN
    CREATE TABLE Contacts
    (
        Id        INT PRIMARY KEY IDENTITY(1,1) ,
        FirstName VARCHAR(255) NOT NULL,
        LastName  VARCHAR(255) NOT NULL,
        Email     VARCHAR(255) NULL,
        Phone     VARCHAR(255) NULL
    );
END;
GO

-- Ensure that either the Email or Phone column is populated
IF OBJECT_ID(N'chk_Contacts_Email_Phone', N'C') IS NULL
BEGIN
    ALTER TABLE Contacts
    ADD CONSTRAINT chk_Contacts_Email_Phone CHECK
    (
        Email IS NOT NULL OR Phone IS NOT NULL
    );
END;
GO

-- Insert some sample data into the Contacts table
IF (SELECT COUNT(*) FROM Contacts) = 0
BEGIN
    INSERT INTO Contacts (FirstName, LastName, Email, Phone)
    VALUES
        ('John', 'Doe', 'john.doe@example.com', '555-123-4567'),
        ('Jane', 'Doe', 'jane.doe@example.com', '555-234-5678');
END;
GO