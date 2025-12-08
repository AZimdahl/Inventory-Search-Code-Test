# Alexander Zimdahl

## Quick Start Instructions

### Prerequisites
- Node.js (v18+)
- npm (v9+)
- .NET 8.0 SDK (if using InventoryServer)

### Steps
1. **Start the backend** (choose one):
  1. Option A: `inventory-mock-api` - See [ReadMe.txt](./inventory-mock-api/ReadMe.txt)
  2. Option B: `InventoryServer`
    1. Navigate to `./InventoryServer/InventoryServer/InventoryServer/` in a powershell terminal
    2. Run"
    ```powershell
    dotnet run
    ```



2. **Start the frontend**:
  1. Navigate to `./Inventory-Search` in a bash terminal
  2. Run:
    ```bash
    npm install
    npm start
    ```

3. **Open the application**: [http://localhost:4200/](http://localhost:4200/)

## Tools Used:
* VS Code
  * Preferred standard code editor
* GitHub Copilot/GitHub Copilot Chat
  * Used to autocomplete lines/blocks to improve speed and accuracy of code
  * Used to clarify intent and direction when stuck
  * Prompted to write base code after getting stuck to correct incorrect code and engineer base level functionality. Then I review the code to make sure it makes sense to me and will refactor as needed to either fix bugs or to resolve other needs/enhance desired behavior
* Google Search
  * Help with knowledge gaps and using unfamiliar concepts
* [Angular Material](https://v18.material.angular.dev/)
  * Installed Angular package for visually better UI components and simpler to implement to use for forms
  * Used for Input, Select, Multiselect, Button, Tooltips, Icons, Table Pagination and Loading Indicators

## TODO:
* Fix:
  * Failing unit tests on FE
* Feat:
  * Set up FE unit tests
* .NET
  * C# Files
  * Test cases