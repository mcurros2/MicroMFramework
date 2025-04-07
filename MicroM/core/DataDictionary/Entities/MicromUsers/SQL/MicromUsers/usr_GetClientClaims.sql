create or alter proc usr_GetClientClaims @username varchar(255) as

-- This query is a place holder for adding the claims that are needed for the client
-- this is application specific and should be customized for the client

-- It is a single record query using the columm name as claim key and the value as value
-- the values should be strings or they will be converted to strings

-- 1. This will be executed upon successful login
-- 2. The results will be added to the claims collection for the user
-- 3. The claims will be sent to the client in the token when loggedin
-- 4. You can use those claims at the client