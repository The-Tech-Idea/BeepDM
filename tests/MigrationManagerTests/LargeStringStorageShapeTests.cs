using Moq;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.DriversConfigurations;
using TheTechIdea.Beep.Helpers.DataTypesHelpers;
using TheTechIdea.Beep.Helpers.UniversalDataSourceHelpers.RdbmsHelpers;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.Editor.Migration.Tests;

public sealed class LargeStringStorageShapeTests
{
    private static IDMEEditor Editor(List<DatatypeMapping> mappings,DataSourceType provider,string handler)
    {
        var config=new Mock<IConfigEditor>();
        config.SetupGet(x=>x.DataTypesMap).Returns(mappings);
        config.SetupGet(x=>x.DataDriversClasses).Returns(new List<ConnectionDriversConfig>{new(){DatasourceType=provider,classHandler=handler}});
        var editor=new Mock<IDMEEditor>();editor.SetupGet(x=>x.ConfigEditor).Returns(config.Object);return editor.Object;
    }
    [Theory]
    [InlineData(128,"varchar(128)")]
    [InlineData(10485760,"varchar(10485760)")]
    [InlineData(10485761,"varchar")]
    [InlineData(16777216,"varchar")]
    public void Postgre_declared_string_capacity_uses_valid_bounded_or_unbounded_variable_storage(int size,string expected)
    {
        var editor=Editor(DatabaseTypeMappingRepository.GetPostgreDataTypesMapping(),DataSourceType.Postgre,"PostgreDataSource");
        var field=new EntityField{FieldName="PayloadJson",Fieldtype="System.String",Size1=size};
        Assert.Equal(expected,DataTypeMappingLookup.GetDataTypeFromDataSourceClassName("PostgreDataSource",field,editor));
        Assert.Equal(expected,DataTypeMappingLookup.GetDataTypeMappingForString("PostgreDataSource","System.String",field,editor).DataType);
    }
    [Theory]
    [InlineData(128,"varchar(128)")]
    [InlineData(10485760,"varchar(10485760)")]
    [InlineData(10485761,"varchar")]
    [InlineData(16777216,"varchar")]
    public void Postgre_migration_helper_uses_the_same_rule_when_configured_with_a_sized_variable_mapping(int size,string expected)
    {
        const string handler="LargePostgreStorageShape";
        var editor=Editor(new(){new(){DataSourceName=handler,NetDataType="System.String",DataType="varchar(N)",Fav=true}},DataSourceType.Postgre,handler);
        Assert.Equal(expected,new RdbmsHelper(editor){SupportedType=DataSourceType.Postgre}.MapClrTypeToDatasourceType(typeof(string),size));
    }
    [Theory]
    [InlineData(4000,"nvarchar(4000)")]
    [InlineData(4001,"nvarchar(max)")]
    [InlineData(16777216,"nvarchar(max)")]
    public void Sql_server_large_unicode_storage_keeps_its_existing_max_rule(int size,string expected)
    {
        var editor=Editor(DatabaseTypeMappingRepository.GenerateSqlServerDataTypesMapping(),DataSourceType.SqlServer,"SQLServerDataSource");
        Assert.Equal(expected,new RdbmsHelper(editor){SupportedType=DataSourceType.SqlServer}.MapClrTypeToDatasourceType(typeof(string),size),ignoreCase:true);
    }
    [Fact]
    public void Postgre_preferred_text_mapping_stays_text_without_a_sized_template()
    {
        var editor=Editor(DatabaseTypeMappingRepository.GetPostgreDataTypesMapping(),DataSourceType.Postgre,"PostgreDataSource");
        Assert.Equal("text",new RdbmsHelper(editor){SupportedType=DataSourceType.Postgre}.MapClrTypeToDatasourceType(typeof(string),16777216));
    }
}
