import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Inventory } from './inventory';

describe('Inventory Component', () => {
  let fixture: ComponentFixture<Inventory>;
  let component: Inventory;

  beforeEach(async () => {
    localStorage.clear();
    await TestBed.configureTestingModule({
      imports: [Inventory],
    }).compileComponents();

    fixture = TestBed.createComponent(Inventory);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  afterEach(() => {
    localStorage.clear();
  });

  it('debe inicializarse con 2 productos por defecto', () => {
    expect(component.inventory().length).toBe(2);
    expect(component.totalItems()).toBe(2);
  });

  it('debe calcular correctamente el valor total del inventario', () => {
    // Laptop: 1200 * 5 = 6000, Monitor: 300 * 12 = 3600 => Total = 9600
    expect(component.totalValue()).toBe(9600);
  });

  it('debe actualizar el stock de un producto con addStock', () => {
    component.addStock('1', 1);
    expect(component.inventory().find(p => p.id === '1')?.quantity).toBe(6);
    // Nuevo total: 1200 * 6 + 300 * 12 = 7200 + 3600 = 10800
    expect(component.totalValue()).toBe(10800);
  });

  it('debe agregar un nuevo producto usando addProduct e incrementar uniqueId', () => {
    component.addProduct('Teclado Mecánico', 100, 2);
    expect(component.inventory().length).toBe(3);
    expect(component.totalItems()).toBe(3);
    expect(component.uniqueId()).toBe(4);
    expect(component.totalValue()).toBe(9600 + 200);
  });

  it('debe persistir los cambios en localStorage a través del effect', async () => {
    component.addStock('1', 2);
    fixture.detectChanges();
    await fixture.whenStable();

    const storedData = localStorage.getItem('dashboard_data');
    expect(storedData).not.toBeNull();
    const parsed = JSON.parse(storedData!);
    expect(parsed.find((p: { id: string }) => p.id === '1').quantity).toBe(7);
  });
});
