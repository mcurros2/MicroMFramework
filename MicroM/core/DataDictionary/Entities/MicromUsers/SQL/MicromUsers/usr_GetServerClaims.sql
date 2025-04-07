create or alter proc usr_GetServerClaims @username varchar(255) as

-- This query is a place holder for adding the impersonation keys used for the client
-- this is application specific and should be customized for the client
-- For MicroMAuthentication impersonation keys always have c_user_id and c_usertype_id included in the key

-- This is a single record query using the column name as key and the value as value
-- The results will be added to the server impersonation keys for the session
-- They keys format is column name that will match the entity column to be overrided, for example c_user_id
-- The value is the value that will be used to override the entity column, for example the c_user_id.value

-- Examples of use: c_person_id, c_customer_id, etc.
-- Those keys will be used to constraint the entities to work for a logged on customer, a logged on person, etc.

-- 1. This will be executed upon successful login
-- 2. The results will be added to the server impersonation keys for the session
-- 3. The keys will be cached for the session duration, including refreshes

