using Xunit;
using Jovemnf.MySQL;

namespace Jovemnf.MySQL.Tests
{
    public class MySQLCriteriaTests
    {
        [Fact]
        public void MySQLCriteria_AddColumn_ShouldAddColumn()
        {
            // Arrange
            var criteria = new MySQLCriteria();
            var columnName = "id";

            // Act
            criteria.AddColumn(columnName);

            // Assert
            Assert.Contains(columnName, criteria.Columns);
        }

        [Fact]
        public void MySQLCriteria_AddColumn_WithAlias_ShouldAddColumnWithAlias()
        {
            // Arrange
            var criteria = new MySQLCriteria();
            var columnName = "user_id";
            var alias = "id";

            // Act
            criteria.AddColumn(columnName, alias);

            // Assert
            Assert.Contains("as " + alias, criteria.Columns);
            Assert.Contains(columnName, criteria.Columns);
        }

        [Fact]
        public void MySQLCriteria_AddColumn_MultipleColumns_ShouldJoinWithComma()
        {
            // Arrange
            var criteria = new MySQLCriteria();
            var column1 = "id";
            var column2 = "name";
            var column3 = "email";

            // Act
            criteria.AddColumn(column1);
            criteria.AddColumn(column2);
            criteria.AddColumn(column3);

            // Assert
            var columns = criteria.Columns;
            Assert.Contains(column1, columns);
            Assert.Contains(column2, columns);
            Assert.Contains(column3, columns);
            Assert.Contains(",", columns);
        }

        [Fact]
        public void MySQLCriteria_Columns_ShouldReturnEmptyString_WhenNoColumnsAdded()
        {
            // Arrange
            var criteria = new MySQLCriteria();

            // Act
            var result = criteria.Columns;

            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        public void MySQLCriteria_AddColumn_WithNullAlias_ShouldAddColumnWithoutAlias()
        {
            // Arrange
            var criteria = new MySQLCriteria();
            var columnName = "id";

            // Act
            criteria.AddColumn(columnName, null);

            // Assert
            Assert.Contains(columnName, criteria.Columns);
            Assert.DoesNotContain("as", criteria.Columns);
        }
    }
}
