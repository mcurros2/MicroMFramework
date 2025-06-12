using LibraryTest.DataDictionary;
using LibraryTest.DataDictionary.CategoriesData;
using MicroM.Configuration;
using MicroM.Core;
using MicroM.Data;
using MicroM.DataDictionary;
using MicroM.DataDictionary.CategoriesDefinitions;
using MicroM.Extensions;
using MicroM.Web.Authentication;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using static LibraryTest.A_DatabaseClientTests;
using static MicroM.Database.DatabaseSchema;

namespace LibraryTest
{
    [TestClass]
    public class C_EntityTests
    {
        [TestMethod]
        [DoNotParallelize]
        public async Task OrderedExecution()
        {
            var init = new EntityTestsUtil();

            await init.DeleteTestDBAsync().ConfigureAwait(false);
            await init.CreateTestDBAsync().ConfigureAwait(false);

            await DataDictionary_InitialConfigurationAsync();
            await DataDictionary_TestAesGSMEncryption();
            await DataDictionary_CreateSchemaAsync();
            await Configure_Categories();
            await Configure_Status();

            await Entity_CreateSchemaAsync();


            await Queue_Insert();
            await Queue_Get();
            await Queue_Update();
            await Queue_Status();
            await Queue_Delete();
            await Queue_Test_Action();
            await Queue_Status();
            await Queue_Delete();
            await Queue_Test_Action();

        }


        public async Task DataDictionary_InitialConfigurationAsync()
        {

            var cts = new CancellationTokenSource();
            var enc = new LocalEncryptor();

            CryptClass.DeleteCertificate(DatabaseConfiguration.CertificateSubjectName);
            string cert_password = CryptClass.CreateRandomPassword();
            using var cert = CryptClass.CreateSelfSignedCertificateAndStoreInUser(cert_password, DatabaseConfiguration.CertificateSubjectName);

            using var client = new DatabaseClient(DatabaseConfiguration.Server, "master", DatabaseConfiguration.user, DatabaseConfiguration.password);

            var cfg = new ConfigurationDB(client, enc);

            cfg.Def.vc_configdatabase.Value = DatabaseConfiguration.ConfigurationDatabase;
            cfg.Def.vc_configsqluser.Value = DatabaseConfiguration.ConfigurationUser;
            cfg.Def.b_recreatedatabase.Value = true;

            ConfigurationDefaults.SecretsFilename = "config_test.cry";

            var common_app_path = Path.Combine(ConfigurationDefaults.SecretsFilePath, ConfigurationDefaults.MicroMCommonID);

            if (!Directory.Exists(common_app_path)) Directory.CreateDirectory(common_app_path);

            string config_path = Path.Combine(common_app_path, ConfigurationDefaults.SecretsFilename);

            if (File.Exists(config_path)) File.Delete(config_path);

            Dictionary<string, object> claims = new()
            {
                { MicroMServerClaimTypes.MicroMUsername, DatabaseConfiguration.user },
                { MicroMServerClaimTypes.MicroMPassword, DatabaseConfiguration.password }
            };

            MicroMOptions options = new()
            {
                ConfigSQLServer = DatabaseConfiguration.Server,
                CertificateThumbprint = cert.Thumbprint,
            };

            var result = await cfg.UpdateData(cts.Token, options: options, server_claims: claims);

            Debug.Print($"Failed: {result.Failed}, {result.Results?[0]?.Message}");

            Assert.IsNotNull(result);
            Assert.AreEqual(false, result.Failed);
            Assert.AreEqual(1, result?.Results?.Count);
            Assert.AreEqual(DBStatusCodes.OK, result?.Results?[0].Status);

            var read_cfg = new ConfigurationDB(client, enc);

            var read_result = await read_cfg.GetData(cts.Token, options, claims);

            Assert.IsNotNull(read_result);
            Assert.IsTrue(read_result);

            Debug.Print($"CertificateThumbprint: {read_cfg.Def.vc_certificatethumbprint.Value}");
            Debug.Print($"Config user: {read_cfg.Def.vc_configsqluser.Value}");
            Debug.Print($"Config path: {config_path}");

            Assert.IsTrue(read_cfg.Def.b_adminuserhasrights.Value);
            Assert.IsTrue(read_cfg.Def.b_configdbexists.Value);
            Assert.IsTrue(read_cfg.Def.b_configuserexists.Value);

        }


        public Task DataDictionary_TestAesGSMEncryption()
        {
            const string CertificateName = "microm_test_certificate";

            CryptClass.DeleteCertificate(CertificateName);

            var pass = CryptClass.CreateRandomPassword();

            using var cert_ret = CryptClass.CreateSelfSignedCertificateAndStoreInUser(pass, CertificateName, 50);

            Assert.IsFalse(string.IsNullOrEmpty(cert_ret.Thumbprint));

            using var cert = CryptClass.FindCertificate(cert_ret.Thumbprint);

            SecretsOptions secrets = new() { ConfigSQLUser = "test_user", ConfigSQLPassword = "test_password" };

            string encrypted = CryptClass.EncryptObject(secrets, cert);

            SecretsOptions unencrypted = CryptClass.DecryptObject<SecretsOptions>(encrypted, cert);

            Assert.IsTrue(secrets.ConfigSQLUser == unencrypted.ConfigSQLUser);
            Assert.IsTrue(secrets.ConfigSQLPassword == unencrypted.ConfigSQLPassword);

            CryptClass.DeleteCertificate(CertificateName);
            return Task.CompletedTask;
        }

        public async Task DataDictionary_CreateSchemaAsync()
        {

            using var client = new DatabaseClient(DatabaseConfiguration.Server, DatabaseConfiguration.TestDatabase, DatabaseConfiguration.user, DatabaseConfiguration.password);
            var cts = new CancellationTokenSource();

            await client.Connect(cts.Token);

            var result = await client.ExecuteSQL("select db_name()", cts.Token);
            string db_name = "";
            if (result.HasData()) db_name = result[0].records[0][0].ToString();

            Assert.AreEqual(db_name, DatabaseConfiguration.TestDatabase, $"Incorrect database found while creating schema. Expecting {DatabaseConfiguration.TestDatabase} found {db_name}");

            await CreateDatadictionarySchemaAndProcs(client, cts.Token);

            var menu = new MainMenuDefinition();
            await menu.AddMenu(client, cts.Token);

            var users = new MicromUsers(client);
            users.Def.vc_username.Value = "admin";
            users.Def.vc_password.Value = "123456";
            users.Def.c_usertype_id.Value = nameof(UserTypes.ADMIN);
            await users.InsertData(cts.Token, true);
        }

        public async Task Configure_Categories()
        {

            using var client = new DatabaseClient(DatabaseConfiguration.Server, DatabaseConfiguration.TestDatabase, DatabaseConfiguration.user, DatabaseConfiguration.password);
            var cts = new CancellationTokenSource();

            await client.Connect(cts.Token);

            var result = await client.ExecuteSQL("select db_name()", cts.Token);
            string db_name = "";
            if (result.HasData()) db_name = result[0].records[0][0].ToString();

            Assert.AreEqual(db_name, DatabaseConfiguration.TestDatabase, $"Incorrect database found while creating schema. Expecting {DatabaseConfiguration.TestDatabase} found {db_name}");

            var test = new QueueType();
            await test.AddCategory(client, cts.Token);


            var check = new Categories(client);
            check.Def.c_category_id.Value = test.CategoryID;
            await check.GetData(cts.Token);

            Assert.AreEqual(check.Def.vc_description.Value, test.Description, $"Incorrect data found testing configuration categories. Expecting {test.Description} found {check.Def.vc_description.Value}");

            var checkv = new CategoriesValues(client);
            checkv.Def.c_category_id.Value = test.CategoryID;
            checkv.Def.c_categoryvalue_id.Value = test.VALUE3.CategoryValueID;
            await checkv.GetData(cts.Token);

            Assert.AreEqual(checkv.Def.vc_description.Value, test.VALUE3.Description, $"Incorrect data found testing configuration categories. Expecting {test.VALUE3.Description} found {checkv.Def.vc_description.Value}");

        }

        public async Task Configure_Status()
        {

            using var client = new DatabaseClient(DatabaseConfiguration.Server, DatabaseConfiguration.TestDatabase, DatabaseConfiguration.user, DatabaseConfiguration.password);
            var cts = new CancellationTokenSource();

            await client.Connect(cts.Token);

            var result = await client.ExecuteSQL("select db_name()", cts.Token);
            string db_name = "";
            if (result.HasData()) db_name = result[0].records[0][0].ToString();

            Assert.AreEqual(db_name, DatabaseConfiguration.TestDatabase, $"Incorrect database found while creating schema. Expecting {DatabaseConfiguration.TestDatabase} found {db_name}");

            var test = new QUEUE();
            await test.AddStatus(client, cts.Token);


            var check = new Status(client);
            check.Def.c_status_id.Value = test.StatusID;
            await check.GetData(cts.Token);

            Assert.AreEqual(check.Def.vc_description.Value, test.Description, $"Incorrect data found testing configuration status. Expecting {test.Description} found {check.Def.vc_description.Value}");

            var checkv = new StatusValues(client);
            checkv.Def.c_status_id.Value = test.StatusID;
            checkv.Def.c_statusvalue_id.Value = test.FAILURE.StatusValueID;
            await checkv.GetData(cts.Token);

            Assert.AreEqual(checkv.Def.vc_description.Value, test.FAILURE.Description, $"Incorrect data found testing configuration status. Expecting {test.FAILURE.Description} found {checkv.Def.vc_description.Value}");

        }

        public async Task Entity_CreateSchemaAsync()
        {

            using var client = new DatabaseClient(DatabaseConfiguration.Server, DatabaseConfiguration.TestDatabase, DatabaseConfiguration.user, DatabaseConfiguration.password);
            var cts = new CancellationTokenSource();

            await client.Connect(cts.Token);

            var result = await client.ExecuteSQL("select db_name()", cts.Token);
            string db_name = "";
            if (result.HasData()) db_name = result[0].records[0][0].ToString();

            Assert.AreEqual(db_name, DatabaseConfiguration.TestDatabase, $"Incorrect database found while creating schema. Expecting {DatabaseConfiguration.TestDatabase} found {db_name}");

            await CreateSchemaAndDictionary<TestQueue>(client, cts.Token, with_iupdate: true, with_idrop: true);
            await CreateSchemaAndDictionary<TestQueueCat>(client, cts.Token);
            await CreateSchemaAndDictionary<TestQueueStatus>(client, cts.Token);
            await CreateSchemaAndDictionary<TestQueueItems>(client, cts.Token, with_iupdate: true, with_idrop: true);

            var asm = Assembly.GetExecutingAssembly();
            await asm.DropAllConstraintsAndIndexes(client, cts.Token);
            await asm.CreateAllConstraintsAndIndexes(client, cts.Token);
            await asm.CreateAssemblyCustomProcs(client, cts.Token);

            await client.Disconnect();
        }


        public async Task Queue_Insert()
        {
            using var client = new DatabaseClient(DatabaseConfiguration.Server, DatabaseConfiguration.TestDatabase, DatabaseConfiguration.user, DatabaseConfiguration.password);
            var cts = new CancellationTokenSource();

            await client.Connect(cts.Token);


            TestQueue test = new(client);
            test.Def.c_queue_id.Value = "1";
            test.Def.vc_description.Value = "Test 1";
            test.Def.c_queuetype_id.Value = nameof(QueueType.VALUE1);
            test.Def.dt_init.Value = DateOnly.FromDateTime(DateTime.Now);
            await test.InsertData(cts.Token);

            await client.Disconnect();

        }

        public async Task Queue_Get()
        {
            using var client = new DatabaseClient(DatabaseConfiguration.Server, DatabaseConfiguration.TestDatabase, DatabaseConfiguration.user, DatabaseConfiguration.password);
            var cts = new CancellationTokenSource();

            var queue = await TestQueue.CreateAndGet(client, "1", cts.Token);


            Assert.AreEqual("Test 1", queue.Def.vc_description.Value);
            Assert.AreEqual(nameof(QueueType.VALUE1), queue.Def.c_queuetype_id.Value.Trim());

        }

        public async Task Queue_Update()
        {
            using var client = new DatabaseClient(DatabaseConfiguration.Server, DatabaseConfiguration.TestDatabase, DatabaseConfiguration.user, DatabaseConfiguration.password);
            var cts = new CancellationTokenSource();

            var cat = await TestQueue.CreateAndGet(client, "1", cts.Token);
            cat.Def.vc_description.Value = "Test 1 Updated";
            await cat.UpdateData(cts.Token);

            var cat2 = await TestQueue.CreateAndGet(client, "1", cts.Token);

            Assert.AreEqual("Test 1 Updated", cat2.Def.vc_description.Value);

        }

        public async Task Queue_Status()
        {
            using var client = new DatabaseClient(DatabaseConfiguration.Server, DatabaseConfiguration.TestDatabase, DatabaseConfiguration.user, DatabaseConfiguration.password);
            var cts = new CancellationTokenSource();


            var queue = await TestQueueStatus.CreateAndGet(client, "1", nameof(QUEUE), cts.Token);

            Assert.AreEqual(nameof(QUEUE.DRAFT), queue.Def.c_statusvalue_id.Value?.Trim());

        }

        public async Task Queue_Delete()
        {
            using var client = new DatabaseClient(DatabaseConfiguration.Server, DatabaseConfiguration.TestDatabase, DatabaseConfiguration.user, DatabaseConfiguration.password);
            var cts = new CancellationTokenSource();

            TestQueue test = new(client);
            test.Def.c_queue_id.Value = "2";
            test.Def.vc_description.Value = "Test 2";
            test.Def.c_queuetype_id.Value = nameof(QueueType.VALUE3);
            await test.InsertData(cts.Token);

            await client.Disconnect();

            var drop = new TestQueue(client);
            drop.Def.c_queue_id.Value = "2";
            await drop.DeleteData(cts.Token);

            var queue = await TestQueue.CreateAndGet(client, "2", cts.Token);

            Assert.AreEqual(null, queue.Def.vc_description.Value);

        }

        public async Task Queue_Test_Action()
        {
            using var client = new DatabaseClient(DatabaseConfiguration.Server, DatabaseConfiguration.TestDatabase, DatabaseConfiguration.user, DatabaseConfiguration.password);
            var cts = new CancellationTokenSource();

            TestQueue test = new(client);

            DataWebAPIRequest args = new()
            {
                Values = { { "c_queue_id", "test" } },
                ServerClaims = new() { { nameof(MicroMServerClaimTypes.MicroMUsername), "admin" }, { nameof(MicroMServerClaimTypes.MicroMPassword), "123456" } }
            };


            var res = (TestQueueActionResult)await test.ExecuteAction(nameof(test.Def.ACTTest), args, new MicroM.Configuration.MicroMOptions() { ConfigSQLServer = "test" }, null, null, cts.Token);

            TestQueueActionResult result = (TestQueueActionResult)res;

            Assert.AreEqual("admin", result.AdminUser);
            Assert.AreEqual("123456", result.AdminPassword);
            Assert.AreEqual("OK", result.TestResult);


        }
    }

}
