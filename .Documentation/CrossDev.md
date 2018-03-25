## Comment démarrer `swissdec` sous `Wine`:

### Ubuntu

- Ouvrir un `Terminal`:

    	<Windows> term <Enter>

- Si ce n'est pas déjà fait, installer une bouteille `salaires`.
	- Télécharger la dernière version de `salaires` depuis [owncloud/snapshots](https://owncloud.epsitec.ch/owncloud/index.php/apps/files/?dir=%2Fsnapshots)
	- Dans la fenêtre `Terminal` taper:
	
			cd Downloads/
			sudo dpkg -i salaires_<version>-snapshot-master-<date>.deb
   
      	La bouteille sera installée dans `/home/.salaires/` qui est un dossier caché (pour le voir dans l'explorateur de fichiers taper `<Control>+H`)

- Ouvrir un `Command Prompt Wine`.
	- Dans la fenêtre `Terminal` taper:
 
    		/opt/salaires/bin/wine wineconsole &

- Si ce n'est pas déjà fait, installer `Mono Windows` dans la bouteille.
	- Télécharger la dernière version de [Mono Windows](http://www.mono-project.com/download/#download-win)
	- Dans le `Command Prompt Wine` taper:
	
			cd \downloads
			msiexec -i mono-4.0.3-gtksharp-2.12.26-win32-0.msi

- Si ce n'est pas déjà fait, installer les *certificats racines* dans `Mono`.
	- Dans le `Command Prompt Wine` taper:
	
			mozroots --import --sync --machine
    
- Les originaux de `salaires` se trouvent dans:

		/opt/salaires/support/salaires/drive_c/Program Files/Cresus/Salaires

- Les exécutables et liens symboliques de `salaires` se trouvent dans:

		/home/<user>/.salaires/salaires/drive_c/Program Files/Cresus/Salaires

### OSX

- Ouvrir un `Command Prompt Wine`.
	- Dans la fenêtre `Terminal` taper:

            (1) open -n -b com.codeweavers.salaires --args wineconsole
            (2) open -n /Applications/Crésus\ Salaires.app --args wineconsole
            (3) /Applications/Crésus\ Salaires.app/Contents/SharedSupport/salaires/bin/wine

- Les originaux de `salaires` (l'application Mac) se trouvent dans:

		/Applications/Crésus Payroll.app/Contents/SharedSupport/salaires/support/salaires/drive_c/Program Files/Cresus/Salaires

- Les exécutables et liens symboliques de `salaires` (le dossier d'exécution) se trouvent dans:

		/Users/<login>/Library/Application Support/Crésus Salaires/Bottles/salaires/drive_c/Program Files/Cresus/Salaires

#### Mise à jour de la bouteille salaires

Comme exemple on va mettre à jour, dans la bouteille, le dossier des exécutables de salaires avec la version `release` bâtie sur une machine Windows.

##### Avec git

- sur la plateforme de développement Windows:
  - bâtir la solution
  - créer un dépôt git dans le dossier de sortie du *build* (ici `<devel>\sal\sal\Win32\v120_xp\bin\Release`)

           cd /d <devel>\sal\sal\Win32\v120_xp\bin\Release
           git init
           git add -A
           git rm --cached **/*.lib **/*.pdb **/*.exp **/*.map
           git commit -m"Initial files"

   - démarrer un démon git basé dans le dossier parent du dossier `release` (ici `<devel>\sal\sal\Win32\v120_xp\bin`)
       - si nécessaire démarrer un shell bash:
   
                start "" "%LocalAppData%\Programs\Git\bin\sh.exe" --login -i
            
        - puis démarrer le démon (s'assurer qu'on est toujours dans le dossier `<devel>\sal\sal\Win32\v120_xp\bin\Release`)
        
                git daemon --base-path=.. --export-all --reuseaddr --informative-errors --verbose

    - noter l'adresse IPv4 de la machine de développement (ici `192.168.1.168`).

- sur le Mac:
    - créer un dépôt git dans le dossier exécutable de salaires (ici `/Users/<login>/Library/Application Support/Crésus Salaires/Bottles/salaires/drive_c/Program Files/Cresus/Salaires`)
    
            cd /Users/<login>/Library/Application\ Support/Crésus\ Salaires/Bottles/salaires/drive_c/Program\ Files/Cresus/Salaires
            git init

    - ajouter un lien (ici `release`) vers le dépôt distant (ici `git://192.168.1.168/release`), puis faire un fetch et un checkout
  
            git remote add release git://192.168.1.168/release
            git fetch release
            git checkout -b release release/master


### OSX vs Ubuntu
```
                 OSX                                                                     Ubuntu
Originaux        /Applications/<app>/Contents/SharedSupport                              /opt
Exécutables      /Users/<user>/Library/Application Support/Crésus Salaires/Bottles       /home/<user>/.salaires
```
#### Contenu de `Crésus Salaires.app`
```
/Applications/<app-name>/Contents/
    SharedSupport/
        salaires/
            bin/
                wine
```
```
/opt/salaires   /Applications/<app-name>/Contents/SharedSupport/salaires 
```
```
export CX_BOTTLE=xxx_
CX_BOTTLE=xxx command_
```    
