using Cvjecara_Latica.Models;

namespace Cvjecara_Latica.Services
{
    public class ZaliheService
    {
        public List<int> ProizvodiBezDovoljnoZaliha(IEnumerable<Cart> stavke)
        {
            ArgumentNullException.ThrowIfNull(stavke);

            var nedostupni = new List<int>();

            foreach (var stavka in stavke)
            {
                if (stavka.ProductQuantity is null || stavka.ProductQuantity <= 0)
                    throw new ArgumentException(
                        $"Neispravna količina za proizvod {stavka.ProductID}.",
                        nameof(stavke));

                if (stavka.ProductQuantity > stavka.Product.Stock)
                    nedostupni.Add(stavka.ProductID);
            }

            return nedostupni;
        }
    }
}
