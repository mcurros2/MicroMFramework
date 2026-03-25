create or alter proc eas_dropUnusedAssemblies as


declare @result int, @msg varchar(255)


begin try

    begin tran

    exec    eas_idropUnusedAssemblies @result out, @msg out

    if @result <> 0
    begin
        rollback
        select  @result, @msg
        return
    end

    commit tran
    select  @result, @msg

end try
begin catch

    if @@TRANCOUNT > 0 begin rollback end;
    throw;

end catch