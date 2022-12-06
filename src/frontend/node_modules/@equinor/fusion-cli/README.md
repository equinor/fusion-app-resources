Fusion CLI
===================

A cli for creating, starting, building, deploying and publishing Fusion apps and tiles

[![oclif](https://img.shields.io/badge/cli-oclif-brightgreen.svg)](https://oclif.io)
[![Version](https://img.shields.io/npm/v/@equinor/fusion-cli.svg)](https://npmjs.org/package/@equinor/fusion-cli)
[![Downloads/week](https://img.shields.io/npm/dw/@equinor/fusion-cli.svg)](https://npmjs.org/package/@equinor/fusion-cli)
[![License](https://img.shields.io/npm/l/@equinor/fusion-cli.svg)](https://github.com/equinor/fusion-cli/blob/master/package.json)

<!-- toc -->
* [Usage](#usage)
* [Commands](#commands)
<!-- tocstop -->
# Usage
<!-- usage -->
```sh-session
$ npm install -g @equinor/fusion-cli
$ fusion COMMAND
running command...
$ fusion (-v|--version|version)
@equinor/fusion-cli/0.0.8 linux-x64 node-v11.1.0
$ fusion --help [COMMAND]
USAGE
  $ fusion COMMAND
...
```
<!-- usagestop -->
# Commands
<!-- commands -->
* [`fusion autocomplete [SHELL]`](#fusion-autocomplete-shell)
* [`fusion build-app`](#fusion-build-app)
* [`fusion create-app`](#fusion-create-app)
* [`fusion help [COMMAND]`](#fusion-help-command)
* [`fusion start-app`](#fusion-start-app)

## `fusion autocomplete [SHELL]`

display autocomplete installation instructions

```
USAGE
  $ fusion autocomplete [SHELL]

ARGUMENTS
  SHELL  shell type

OPTIONS
  -r, --refresh-cache  Refresh cache (ignores displaying instructions)

EXAMPLES
  $ fusion autocomplete
  $ fusion autocomplete bash
  $ fusion autocomplete zsh
  $ fusion autocomplete --refresh-cache
```

_See code: [@oclif/plugin-autocomplete](https://github.com/oclif/plugin-autocomplete/blob/v0.1.2/src/commands/autocomplete/index.ts)_

## `fusion build-app`

Build the app as a ready-to-deply zip bundle

```
USAGE
  $ fusion build-app

OPTIONS
  -o, --out=out   [default: ./out] Output path
  -s, --silent    No console output
  -z, --[no-]zip  Generate zip
```

_See code: [src/commands/build-app.ts](https://github.com/equinor/fusion-cli/blob/v0.0.8/src/commands/build-app.ts)_

## `fusion create-app`

Creates a new fusion app

```
USAGE
  $ fusion create-app

OPTIONS
  -N, --shortName=shortName      App short name
  -d, --description=description  App description
  -g, --git                      Initialize git repository
  -h, --help                     show CLI help
  -i, --install                  Install dev dependencies
  -k, --key=key                  Key for app/tile
  -n, --name=name                Name for app/tile(use quotes for spaces)
```

_See code: [src/commands/create-app.ts](https://github.com/equinor/fusion-cli/blob/v0.0.8/src/commands/create-app.ts)_

## `fusion help [COMMAND]`

display help for fusion

```
USAGE
  $ fusion help [COMMAND]

ARGUMENTS
  COMMAND  command to show help for

OPTIONS
  --all  see all commands in CLI
```

_See code: [@oclif/plugin-help](https://github.com/oclif/plugin-help/blob/v2.1.6/src/commands/help.ts)_

## `fusion start-app`

Start a fusion app

```
USAGE
  $ fusion start-app

OPTIONS
  -P, --production  Use production config
  -a, --apps=apps   Compile one or more fusion apps. E.g. --apps AppKey1 AppKey2 AppKey3
  -h, --help        show CLI help
  -p, --progress    Display build progress
```

_See code: [src/commands/start-app.ts](https://github.com/equinor/fusion-cli/blob/v0.0.8/src/commands/start-app.ts)_
<!-- commandsstop -->
