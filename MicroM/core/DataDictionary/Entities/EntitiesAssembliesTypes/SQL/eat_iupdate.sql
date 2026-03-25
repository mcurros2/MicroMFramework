create or alter proc eat_iupdate
        @assembly_id Char(20)
		, @assemblytype_id Char(20)
		, @assemblytypename VarChar(2048)
		, @lu DateTime
		, @webusr VarChar(80)
        , @result int output
        , @msg varchar(255) output
        as

if @@trancount = 0 throw 50001, 'eat_iupdate must be called within a transaction', 1

begin try
    declare @cu datetime, @now datetime=getdate(), @login sysname=original_login()

    -- MMC: ensure we are not adding an existing type
    select  @cu=dt_lu
    from    [entities_assemblies_types] with (rowlock, holdlock, updlock)
    where   c_assembly_id = @assembly_id
			and vc_assemblytypename = @assemblytypename

    if @cu is null
    begin
        
        declare @id bigint
		exec num_iGetNewNumber 'eat', @nextnumber = @id out
		select @assemblytype_id = right('0000000000'+rtrim(@id),10)

        insert  [entities_assemblies_types]
        values
            (
            @assembly_id
			, @assemblytype_id
			, @assemblytypename
            , @now
            , @now
            , @webusr
            , @webusr
            , @login
            , @login
            )
        
        
        select	@result = 15, @msg = rtrim(@assemblytype_id)
        return
    end
    
    -- MMC: no update just adds

    select @result = 0, @msg = 'OK'
    
end try
begin catch

    throw;

end catch