namespace Minesweeper.Extensions;

public static class StringExtension
{
    public static void ValidateLettersOrNumbers(this string input)
    {
        // Nejprve zkontrolujeme, zda řetězec není prázdný nebo neobsahuje jen bílé znaky
        if (string.IsNullOrWhiteSpace(input))
        {
            throw new ArgumentException("{input} řetězec je prázdný!", input);
        }

        // Pak zkontrolujeme, že všechny znaky jsou buď písmena nebo číslice
        if (!input.All(char.IsLetterOrDigit))
        {
            throw new ArgumentException("{input} obsahuje nepovolené znaky!", input);
        }
    }
}
