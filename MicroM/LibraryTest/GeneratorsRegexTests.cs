using MicroM.Generators;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace LibraryTest
{
    [TestClass]
    public class GeneratorsRegexTests
    {

        [TestMethod]
        public void ReduceMultipleEmptyLinesToOne_ShouldReduceEmptyLinesCorrectly()
        {

            var input = @"create or alter proc cat_drop
        @category_id Char(20)
        as

begin try

    begin tran




    delete  [categories]
    where   c_category_id = @category_id

    commit tran
    select  0, 'OK'




end try
begin catch

    if @@TRANCOUNT > 0
    begin
        rollback
    end;

    throw;

end catch";

            var expected =
                @"create or alter proc cat_drop
        @category_id Char(20)
        as

begin try

    begin tran

    delete  [categories]
    where   c_category_id = @category_id

    commit tran
    select  0, 'OK'

end try
begin catch

    if @@TRANCOUNT > 0
    begin
        rollback
    end;

    throw;

end catch";


            var result = GeneratorsRegex.MultipleEmptyLines().Replace(input, Environment.NewLine + Environment.NewLine);


            Assert.AreEqual(expected, result);
        }
    }
}
