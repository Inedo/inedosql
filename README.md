# inedosql

[![Build status](https://buildmaster.inedo.com/api/ci-badges/image?API_Key=badges&$ApplicationId=64)](https://buildmaster.inedo.com/api/ci-badges/link?API_Key=badges&$ApplicationId=64)

## Using inedosql

 - inedosql is used primarily to execute `.sql` script files against a SQL Server database
 - this is done with the `update` command (see "Update")
 - these sql files can be executed from disk, or an embedded/attached zip file (see "Embedding scripts for distribution")
 - scripts are executed in lexical order (i.e. alphabetically), which means you can use directory structures and prefixes to properly order scripts (see "Example Scripts Layout")
 - you can mark a script as a "tracked" script, which means it won't always be executed

## Tracked and Untracked SQL Scripts

inedosql is intended for use with lexically-ordered scripts in `.sql` files. When using the `update` command
(see below), it will iterate all `.sql` files in a directory recursively, possibly executing it against
the target database depending on whether it is a tracked or untracked script.

A **tracked** script, is a `.sql` script with a special comment header on the first line using the following format:

    --AH:ScriptId=<unique-guid>[;ExecutionMode=<Once|OnChange|Always>][;UseTransaction=<True|False>]

Each tracked script's GUID uniquely identifies it and allows its executions against a target database to
be recorded. This allows one-time database schema changes to be executed once and only once against a database.

The `ExecutionMode` value is optional, and may be one of:
 - **Once**: The script is only ever run once. This is the default behavior if `ExecutionMode` is not specified.
 - **OnChange**: The script is run only when it has changed compared to the last time it was executed against the target database.
 - **Always**: The script is run every time like an untracked script, but the success/failure of its last run is retained because it is tracked.

The `UseTransaction` value is optional, and may be one of:
 - **True**: The script is run using a transaction, and will rollback on an error. This is the default behavior if `UseTransaction` is not specified.
 - **False**: The script will not use a transaction; this may be required for certain scripts, including ALTER DATABASE statements.
 
An **untracked** script is simple a plain `.sql` script without the header. These scripts are run every time,
and nothing is persisted about each run.

### Examples of Tracked Scripts

This uses the implicit `ExecutionMode=Once` to ensure that the `[MyTable]` table
is only created one time:

    --AH:ScriptId=E632420E-0D90-46F7-A23E-1F374786CABD
    CREATE TABLE [MyTable] ([MyColumn] INT)

An example of a "cleanup script" to remove old views. It can be run multiple times, but doesn't need to be
re-run unless it's been modified, so it uses `ExecutionMode=OnChange`:    

    --AH:ScriptId=5428C9CD-59DD-40B7-BF39-87259CDF7653;ExecutionMode=OnChange
    IF OBJECT_ID('MyOldView') IS NOT NULL DROP VIEW [MyOldView]

### Tracked Script Metadata Table

Information about tracked scripts are stored in the database itself, in a table called `__InedoDb_DbSchemaChanges`. See [Initialize.sql](https://github.com/Inedo/inedosql/blob/master/inedosql/SqlServer/Scripts/Initialize.sql) for the structure.

## Example Scripts Layout

The below example shows how you can use both directories and numeric prefixes ensure proper execution order.
Procedures can use Views, which in turn can use functions, but usually not the other way around. Since a handful
database objects depend on other objects, those can prefixed with "2." instead of "1."

    1-SCHEMA\
        001.001\
            1.initial-schema.sql
        001.002\
            1.data-patch.orders.sql
        002.000\
            1.fix-orders-table.sql
    2-OBJECTS\
       1-TRIGGERS\
            1.ValidateOrders.sql
       2-FUNCTIONS\
            1.FormatOrderNumber.sql
       3-VIEWS\
            1.OrdersWithTotals.sql
            1.SalesReport.sql
            2.OrdersPastDue.sql
       4-PROCS\
            1.AddItemToOrder.sql
            1.ValidateOrder.sql
            2.ProcessOrder.sql

## Command Line Reference

    inedosql <command> [--connection-string=<connection-string>] [options...]

`<connection-string>` is required, and must contain a valid SQL Server connection string that specifies a database.

`<command>` must be one of:

### update

    inedosql update <script-path> [--force]

Executes all `.sql` files in `<script-path>`, recursively iterating through all subdirectories.
Scripts are executed in lexical order.

When a script execution fails, no further scripts will be executed; inedosql will terminate immediately.
If a tracked script fails, the error is recorded and future `update` commands executed against the database
will fail until the error has been resolved or unless the `--force` option is specified.

### errors

    inedosql errors [--all]

Lists all tracked scripts with unresolved errors. If `--all` is specified, tracked scripts with resolved
errors are also displayed.

### error

    inedosql error <script-guid-or-name>

Displays details about a tracked script execution error. `<script-guid-or-name>` may be either the
tracked script's unique ID or its file name relative to the execution directory. Note that script names
are not guaranteed to be unique; if there are multiple scripts executed of the same name, specifying
the name will have undefined results.

### script

    inedosql script <script-guid-or-name>

Displays details about a tracked script execution, including the executed script itself if it was stored.
`<script-guid-or-name>` may be either the tracked script's unique ID or its file name relative to
the execution directory. Note that script names are not guaranteed to be unique; if there are multiple
scripts executed of the same name, specifying the name will have undefined results.

### resolve-error

    inedosql resolve-error [<script-guid>] [--all] [--comment=<comment>]

Marks one or more tracked script execution errors as resolved, which will allow the `update` command
to execute scripts without specifying the `--force` option.

If `<script-guid>` is specified, this command applies to a recorded error of a specific tracked
script, and `<script-guid>` must be the unique ID of the script.

If `--all` is specified, this command applies to all unresolved tracked script errors. Specifying
`<script-guid>` and `--all` together is invalid.

`<comment>` is optional, and if specified, provides a short comment to be recorded with the
resolution for the script.

## Embedding scripts for distribution

If you need to distribute database scripts using this tool in a single executable file, you can zip
all of your .sql scripts and then append the zip file onto `inedosql.exe`:

    copy /b inedosql.exe+myscripts.zip MyScripts.exe

To prevent confusion, you should probably not name the distribution exe "inedosql.exe."
