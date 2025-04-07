create or alter proc apa_iupdateAssembly
        @application_id Char(20)
		, @assembly_id Char(20)
		, @assembly_path VarChar(2048)
		, @order int
		, @lu DateTime
		, @webusr VarChar(80)
        , @result int output
        , @msg varchar(255) output
        as

if @@trancount = 0 throw 50001, 'apa_iupdateAssembly must be called within a transaction', 1

-- This proc is used to update assemblies while adding/updating a new app

-- The tables involved are
--		entities_assemblies (eas)
--		applications_assemblies (apa)

-- The UI has the option to add/edit up to 5 assemblies per app
-- The order parm @order indicates assembly #1, #2, etc.
-- Here we do:
-- 1. Check to see if @assembly_path is '' this means that we MAY need to drop the relation with the app (apa_idrop)
--    and if the assembly is not referenced by any app, drop the assembly (eas_idrop)
-- 2. If the assembly path is not '' then we may need to insert the assembly (eas_iupdate) and/or add the realtion with the app (apa_iupdate)

begin try

	-- If @assembly_path is '' then we may need to drop the relation and assembly
	if isnull(@assembly_path,'') = ''
	begin
		-- Get the assembly ID from the order field
		select	@assembly_id=null
		select	@assembly_id=a.c_assembly_id
		from	applications_assemblies a
		where	a.c_application_id=@application_id
				and a.i_order=@order

		-- If @assembly_id is not null, then we perform the delete
		-- If it IS null then it means the proc is just called with an empty value an we shoul ignore it
		if @assembly_id is not null
		begin
			-- Drop the relation between the assembly and the application
			exec apa_idrop @application_id, @assembly_id, @result out, @msg out

			if @result <> 0
			begin
				return
			end

			-- Check if the assembly is still related to any application
			if not exists
			(
				select	*
				from	applications_assemblies a
				where	a.c_assembly_id=@assembly_id
			)
			begin
				-- Drop the assembly as it's not related to any application
				exec eas_idrop @assembly_id, @result out, @msg out

				if @result <> 0
				begin
					return
				end
			end
		end

		select	@result=0
		return
	end

	-- If @assembly_id is null, we are inerting a new assembly. 
	-- We try to find if the @assembly_id with the @assembly_path already exists (the assembly was created by another app)
	if @assembly_id is null
	begin
		select	@assembly_id = a.c_assembly_id,
				@lu = a.dt_lu
		from	entities_assemblies a
		where	a.vc_assemblypath = @assembly_path
	end

	-- We are now sure we need to create a new assembly
	if @assembly_id is null
	begin

		-- Insert the new assembly in entities_assemblies. Insert result codes are 15 for a new record, 0 for an existing record
		exec eas_iupdate null, @assembly_path, null, @webusr, @result out, @msg out
 
		if @result <> 15
		begin
			return
		end

		-- Insert the relation between assemblies and applications
		set	 @result=-1
		exec apa_iupdate @application_id, @msg, @order, null, @webusr, @result out, @msg out    

		if @result <> 0
		begin
			return
		end

	end
	else
	begin
		-- We are going to update the assembly and it's relation
		declare	@i_lu datetime

		select	@i_lu=a.dt_lu
		from	entities_assemblies a
		where	a.c_assembly_id=@assembly_id

		-- Update the assembly_path. This can yield an error if the assembly exists
		exec eas_iupdate @assembly_id, @assembly_path, @i_lu, @webusr, @result out, @msg out
 
		if @result <> 0
		begin
			return
		end

		select	@i_lu=a.dt_lu
		from	applications_assemblies a
		where	a.c_assembly_id=@assembly_id
				and a.c_application_id=@application_id

		-- Create the relation if not exists
		set	 @result=-1
		exec apa_iupdate @application_id, @assembly_id, @order, @i_lu, @webusr, @result out, @msg out    

		if @result <> 0
		begin
			return
		end

	end

end try
begin catch

    throw;

end catch
