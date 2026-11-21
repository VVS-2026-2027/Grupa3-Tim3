using Cvjecara_Latica.Models;

namespace Cvjecara_Latica.Services
{
    /// <summary>
    /// Provjerava da li ima dovoljno zaliha za stavke iz korpe prije narudžbe.
    /// Razvijeno TDD pristupom (Task 3).
    /// </summary>
    public class ZaliheService
    {
        public List<int> ProizvodiBezDovoljnoZaliha(IEnumerable<Cart> stavke)
        {
            ArgumentNullException.ThrowIfNull(stavke);

            var listaStavki = stavke.ToList();

            foreach (var stavka in listaStavki)
                ProvjeriKolicinu(stavka);

            return listaStavki
                .Where(PrelaziZalihu)
                .Select(s => s.ProductID)
                .ToList();
        }

        private static void ProvjeriKolicinu(Cart stavka)
        {
            if (stavka.ProductQuantity is null || stavka.ProductQuantity <= 0)
                throw new ArgumentException($"Neispravna količina za proizvod {stavka.ProductID}.", "stavke");
        }

        private static bool PrelaziZalihu(Cart stavka) =>
            stavka.ProductQuantity > stavka.Product.Stock;
    }
}