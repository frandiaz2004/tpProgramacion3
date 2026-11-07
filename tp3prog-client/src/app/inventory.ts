import { Component, computed, effect, signal } from '@angular/core';

export interface Product {
  id: string;
  name: string;
  price: number;
  quantity: number;
}

@Component({
  selector: 'app-inventory',
  templateUrl: './inventory.html',
  styleUrl: './inventory.scss',
})
export class Inventory {
  // Inicializando el Signal con datos base
  readonly inventory = signal<Product[]>([
    { id: '1', name: 'Laptop', price: 1200, quantity: 5 },
    { id: '2', name: 'Monitor', price: 300, quantity: 12 },
  ]);

  // Contador autoincremental para nuevos productos
  readonly uniqueId = signal<number>(3);

  // Se recalcula automáticamente si cambia el 'inventory'
  readonly totalValue = computed(() => {
    return this.inventory().reduce(
      (acc, item) => acc + item.price * item.quantity,
      0
    );
  });

  readonly totalItems = computed(() => this.inventory().length);

  constructor() {
    // El efecto guarda los datos automáticamente
    effect(() => {
      const data = JSON.stringify(this.inventory());
      if (typeof localStorage !== 'undefined') {
        localStorage.setItem('dashboard_data', data);
      }
    });
  }

  addStock(productId: string, amount: number): void {
    this.inventory.update(products =>
      products.map(p =>
        p.id === productId ? { ...p, quantity: p.quantity + amount } : p
      )
    );
  }

  // Método opcional para añadir nuevos productos con ID autoincremental
  addProduct(name: string, price: number, quantity: number = 1): void {
    const currentId = this.uniqueId().toString();
    const newProduct: Product = {
      id: currentId,
      name,
      price,
      quantity,
    };

    // Incrementa uniqueId con update
    this.uniqueId.update(id => id + 1);

    // Agrega el nuevo producto a products mediante update
    this.inventory.update(list => [...list, newProduct]);
  }
}
