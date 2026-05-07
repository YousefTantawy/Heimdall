-- Create Roles table first
CREATE TABLE IF NOT EXISTS "Roles" (
    "RoleId" SERIAL PRIMARY KEY,
    "Name" TEXT NOT NULL
);

-- Create Users table with Foreign Key mapping
CREATE TABLE IF NOT EXISTS "Users" (
    "UserId" SERIAL PRIMARY KEY,
    "Username" TEXT NOT NULL,
    "Email" TEXT NOT NULL,
    "PasswordHash" TEXT NOT NULL,
    "RoleId" INTEGER NOT NULL,
    CONSTRAINT "FK_Users_Roles_RoleId" FOREIGN KEY ("RoleId") 
        REFERENCES "Roles" ("RoleId") ON DELETE CASCADE
);

-- Seed the foundational roles
INSERT INTO "Roles" ("RoleId", "Name") VALUES 
(1, 'User'), 
(2, 'Admin') 
ON CONFLICT ("RoleId") DO NOTHING;