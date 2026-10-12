import { HttpClient } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { FormsModule, NgForm } from '@angular/forms';
import { forkJoin } from 'rxjs';

interface Stats {
  owners: number;
  pets: number;
  vets: number;
}

interface Pet {
  id: number;
  name: string;
  type: string;
  birthDate: string;
}

interface Owner {
  id: number;
  firstName: string;
  lastName: string;
  address: string;
  city: string;
  telephone: string;
  pets: Pet[];
}

interface Vet {
  id: number;
  firstName: string;
  lastName: string;
  specialty: string;
}

interface NewOwner {
  firstName: string;
  lastName: string;
  address: string;
  city: string;
  telephone: string;
}

@Component({
  selector: 'app-root',
  imports: [FormsModule],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App {
  private readonly http = inject(HttpClient);

  protected readonly stats = signal<Stats | null>(null);
  protected readonly owners = signal<Owner[]>([]);
  protected readonly vets = signal<Vet[]>([]);
  protected readonly loading = signal(true);
  protected readonly saving = signal(false);
  protected readonly error = signal('');
  protected newOwner: NewOwner = this.emptyOwner();

  constructor() {
    this.load();
  }

  protected addOwner(form: NgForm): void {
    if (form.invalid || this.saving()) {
      return;
    }

    this.saving.set(true);
    this.error.set('');
    this.http.post<Owner>('/api/owners', this.newOwner).subscribe({
      next: () => {
        this.newOwner = this.emptyOwner();
        form.resetForm(this.newOwner);
        this.saving.set(false);
        this.load();
      },
      error: () => {
        this.error.set('The owner could not be saved. Check the API resource logs in the Aspire dashboard.');
        this.saving.set(false);
      }
    });
  }

  private load(): void {
    this.loading.set(true);
    this.error.set('');

    forkJoin({
      stats: this.http.get<Stats>('/api/stats'),
      owners: this.http.get<Owner[]>('/api/owners'),
      vets: this.http.get<Vet[]>('/api/vets')
    }).subscribe({
      next: ({ stats, owners, vets }) => {
        this.stats.set(stats);
        this.owners.set(owners);
        this.vets.set(vets);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Petclinic data is unavailable. Check the API and PostgreSQL resources in the Aspire dashboard.');
        this.loading.set(false);
      }
    });
  }

  private emptyOwner(): NewOwner {
    return { firstName: '', lastName: '', address: '', city: '', telephone: '' };
  }
}
