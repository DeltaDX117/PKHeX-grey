PKHeX is a Windows program, and you build it yourself from the fork. You need two things installed once: **Git** and the **.NET 10 SDK**. Get the SDK from https://dotnet.microsoft.com/download/dotnet/10.0, using the "SDK x64" installer for Windows; the runtime alone isn't enough to build.

Then open **PowerShell** or **Command Prompt** (not Python), and run these one at a time.

**1. Download your fork** into a folder of your choice:

```bash
git clone https://github.com/DeltaDX117/PKHeX-grey
```

```bash
cd PKHeX-grey
```

**2. Switch to the branch with Grey support:**

```bash
git checkout grey-saves
```

**3. Build it and start it:**

```bash
dotnet run --project PKHeX.WinForms -c Release
```

The first build takes a minute or two; after that PKHeX opens.

**For a normal PKHeX.exe** you can double-click later without the command line, build it into a folder once:

```bash
dotnet publish PKHeX.WinForms -c Release -o PKHeX-out
```

Then `PKHeX-out\PKHeX.exe` is the program; you can make a desktop shortcut to it.

**Opening your Grey save:** in melonDS, the save is the `.sav` file next to your ROM, with the same name. Open it in PKHeX with File → Open, or drag it onto the window. Save your edits with File → Export SAV, then give melonDS that file. Close the game in melonDS first, and keep a copy of the original save to be safe.

**Updating later,** after I push more changes or once you've merged them into `master`:

```bash
git pull
```

Then build again with the run or publish command.

If anything fails, copy the error text here.

The `grey-saves` commit was already on GitHub. The stop hook flagged it because my local copy wasn't linked to the remote branch; it is now, and there's nothing left to push. The setup steps in my previous message are unchanged.