using Cvjecara_Latica.Models;

namespace Cvjecara_Latica.Services
{
    public class ZaliheService
    {
        public List<int> ProizvodiBezDovoljnoZaliha(IEnumerable<Cart> stavke)
        {
            ArgumentNullException.ThrowIfNull(stavke);

            return new List<int>();
        }
    }
}
