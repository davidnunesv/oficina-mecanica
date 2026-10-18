using Oficina.Domain.Enums;
using Oficina.Domain.ValueObjects;

namespace Oficina.UnitTests.ValueObjects
{
    public class DocumentoTests
    {
        [Theory]
        [InlineData("529.982.247-25", TipoDocumento.Cpf)]
        [InlineData("52998224725", TipoDocumento.Cpf)]
        [InlineData("11.222.333/0001-81", TipoDocumento.Cnpj)]
        [InlineData("12.ABC.345/01DE-35", TipoDocumento.Cnpj)]
        public void DeveAceitarDocumentoValido(string valor,TipoDocumento tipoEsperado)
        {
            // Arrange e Act
            var documento = new Documento(valor);

            // Assert
            Assert.Equal(tipoEsperado, documento.Tipo);
        }

        [Theory]
        [InlineData("123")]
        [InlineData("11111111111")]
        [InlineData("52998224726")]
        [InlineData("11.222.333/0001-87")]
        [InlineData("12.ABC.345/01DE-36")]
        [InlineData("12.AB@.345/01DE-35")]
        public void DeveRejeitarDocumentoInvalido(string valor)
        {
            // Act e Assert
            Assert.Throws<ArgumentException>(
                () => new Documento(valor));
        }

        [Fact]
        public void DocumentosComMesmoValorDevemSerIguais()
        {
            // Arrange
            var documento1 = new Documento("529.982.247-25");
            var documento2 = new Documento("52998224725");

            // Assert
            Assert.Equal(documento1, documento2);
        }

        [Fact]
        public void DeveNormalizarCnpjAlfanumerico()
        {
            // Act
            var documento = new Documento("12.abc.345/01de-35");

            // Assert
            Assert.Equal("12ABC34501DE35", documento.Valor);
            Assert.Equal(TipoDocumento.Cnpj, documento.Tipo);
        }
    }
}
