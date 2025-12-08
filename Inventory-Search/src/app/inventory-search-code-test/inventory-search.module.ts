//inventory-search.module.ts
// TypeScript
import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule } from '@angular/forms';
import { HttpClientModule } from '@angular/common/http';
import { IndexPageComponent } from './pages/index-page/index-page.component';
import { ResultsTableComponent } from './components/results-table/results-table.component';
import { INVENTORY_API_BASE } from './services/inventory-search-api.service';

// import Angular Material modules used in components
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';
import { MatInputModule } from '@angular/material/input';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';

@NgModule({
  declarations: [IndexPageComponent, ResultsTableComponent],
  imports: [
    CommonModule,
    ReactiveFormsModule,
    HttpClientModule,
    MatFormFieldModule,
    MatSelectModule,
    MatInputModule,
    MatCheckboxModule,
    MatButtonModule,
    MatIconModule,
    MatTooltipModule
  ],
  exports: [IndexPageComponent],
  providers: [
    // Provide a default; override in app.module.ts for real environments.
    { provide: INVENTORY_API_BASE, useValue: '/api' }
  ]
})
export class InventorySearchCodeTestModule { }
