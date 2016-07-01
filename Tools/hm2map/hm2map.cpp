#include "stdafx.h"
#include  <io.h>

void usage(const char *progname)
{
  fprintf (stderr, "usage: %s hmfile [htmldir synonymfile]", progname) ;
  exit(1) ;
}
BOOL GetLine(FILE *input, CString & str)
{
  char line[1000] ;


  if (fgets(line, 1000, input))
  {
    line[strlen(line) - 1] = 0 ;

    str = line ;

    str.TrimLeft() ;
    str.TrimRight() ;

    int commentpos = str.Find("//") ;

    if (commentpos != -1)
    {
      str = str.Left(commentpos) ;
    }
    return TRUE ;
  }
  else
    return FALSE ;
}
BOOL GetSymbols(CString & s, CString & symbol, CString & hex)
{
  symbol.Empty() ;
  hex.Empty() ;

  if (s.GetLength() == 0)
    return FALSE ;

  BOOL insymbol = TRUE ;

  int i ;
  for (i = 0; i < s.GetLength(); i++)
  {
    char c = s[i] ;

    if (isspace(c))
    {
      insymbol = false ;

      while (i < s.GetLength() && isspace(s[i]) )
        i++ ;

      i-- ;
      c = s[i] ;
    }

    if (insymbol)
    {
      symbol += c ;
    }
    else
    {
      hex += c ;
    }
  }

  symbol.TrimLeft() ;
  symbol.TrimRight() ;
  hex.TrimRight() ;
  hex.TrimLeft() ;

  return TRUE ;
}

int main (int argc, char *argv[])
{
  int retval = 0 ;

  CString htmldir ;

  FILE *input ;

  if (argc < 2 || argc == 3 || argc > 4)
  {
    usage(argv[0]) ;
  }


  input = fopen (argv[1], "r") ;
  if (input == NULL)
  {
    fprintf (stderr, "Error: Can't open %s\n", argv[1]) ;
    return 1 ;
  }


  CMapStringToString synonymsmap ;
  CMapStringToString symbolmap ;
  CMapStringToString reversesymbolmap ;

  if (argc == 4)
  {
    // [htmldir] spécifié
    char dir[_MAX_PATH] ;

    GetCurrentDirectory(_MAX_PATH, dir) ;

    if (SetCurrentDirectory(argv[2]))
    {
      htmldir = argv[2] ;
      SetCurrentDirectory(dir) ;

      if (htmldir.Right(1) != "\\")
        htmldir += "\\" ;
    }
    else
    {
      fprintf (stderr, "Error: Can't SetDiretory to \"%s\"", argv[2]) ;
      return 1 ;
    }

    FILE *input ;

    input = fopen (argv[3], "r") ;
    if (input == NULL)
    {
      fprintf (stderr, "Error: Can't open synonymfile %s\n", argv[3]) ;
      return 1 ;
    }

    CString s, symbol, synonym ;
    while (GetLine(input, s))
    {
      if (GetSymbols(s, symbol, synonym))
      {
        CString bidon ;
        if (synonymsmap.Lookup(symbol, bidon))
        {
          fprintf(stderr, "Error: Synonyme \"%s\" déjà défini\n", symbol) ;
          exit(1) ;
        }

        synonymsmap.SetAt(symbol, synonym) ;
      }
    }

    fclose(input) ;
    
  }


  CString s ;
  while (GetLine(input, s))
  {
    CString symbol ;
    CString hex ;

    if (GetSymbols(s, symbol, hex))
    {
      symbol.MakeLower() ;
      CString bidon ;
      if (symbolmap.Lookup(hex, bidon))
      {
        fprintf(stderr, "Warning: symbole \"%s\" à double : %s = %s\n", symbol, bidon, hex) ;
      }
      else
      {
        symbolmap.SetAt(hex, symbol) ;
        reversesymbolmap.SetAt(symbol, hex) ;
      }
    }
  }


  printf ("// Fichier généré automatiquement par hm2mapn à partir du fichier %s\n", argv[1]) ;
  printf ("// Ne pas modifier à la main.\n\n") ;
  printf ("#include \"stdafx.h\"\n\n");
  printf ("#include \"helpmap.h\"\n\n");

  printf ("struct HelpMap HelpMap[] =\n") ;
  printf ("{\n") ;

  POSITION pos ;
  CString symbol, hex ;

  for (pos = symbolmap.GetStartPosition() ; pos != NULL ;)
  {
    symbolmap.GetNextAssoc(pos, hex, symbol) ;

    if (symbol == "hid_edit_effacegroup") // || symbol == "hid_compta_remettrbudg")
    {
      TRACE("*") ;
    }

    CString htmlfile = htmldir + symbol + ".htm" ;

    CString synonym ;
    if (synonymsmap.Lookup(symbol, synonym))
    {
      CString synonymfile = htmldir + synonym + ".htm" ;

      if (_access(htmlfile, 04) == 0)
      {
        fprintf (stderr, "Error: Synonyme \"%s\" inutile car fichier \"%s\" existe\n", synonym, htmlfile) ;
        retval = 1 ;
      }

      CString synhex ;
      if (reversesymbolmap.Lookup(synonym, synhex))
      {
        if (_access(synonymfile, 04) == 0)
        {
          symbolmap.SetAt(hex, synonym) ;
        }
        else
        {
          TRACE("*") ;
        }
      }
      else
      {
        if (_access(synonymfile, 04) != 0)
        {
          char dir[_MAX_PATH] ;
          GetCurrentDirectory(_MAX_PATH, dir) ;
          fprintf (stderr, "Error: Fichier html \"%s\" n'existe pas\n", htmlfile) ;
          retval = 1 ;
        }
        else
        {
          symbolmap.SetAt(hex, synonym) ;
        }
      }
    }
    else
    {
      if (_access(htmlfile, 04) != 0)
      {
        TRACE("REMOVED: %s  %s\n", symbol, htmlfile) ;
        CString hex ;
        reversesymbolmap.Lookup(symbol, hex) ;
        symbolmap.RemoveKey(hex) ;
      }
      else
      {
      }
    }
  }


  for (pos = symbolmap.GetStartPosition() ; pos != NULL ;)
  {
    symbolmap.GetNextAssoc(pos, hex, symbol) ;
    printf ("\t{%s, \"%s\" },\n", hex, symbol) ;
  }



  printf ("\t{0, NULL}\n} ;") ;



  return retval ;
}