import { Component, OnInit, OnDestroy, NgModule } from '@angular/core';
import { ButtonModule } from 'primeng/button';
import { CheckboxModule } from 'primeng/checkbox';
import { CommonModule } from '@angular/common';
import {
  TableModule,
  TableRowCollapseEvent,
  TableRowExpandEvent,
} from 'primeng/table';
import { InputTextModule } from 'primeng/inputtext';
import { DialogModule } from 'primeng/dialog';
import { ToastModule } from 'primeng/toast';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import {
  FormBuilder,
  FormGroup,
  FormsModule,
  ReactiveFormsModule,
  Validators,
} from '@angular/forms';
import { DropdownModule } from 'primeng/dropdown';
import { ConfirmationService, MessageService } from 'primeng/api';
import { PaginatorModule } from 'primeng/paginator';
import { Subscription } from 'rxjs';
import { ProductService } from '../../../_services/product.service';
import { Router, RouterModule } from '@angular/router';
import { CategoryService } from '../../../_services/category.service';
import { SelectButtonModule } from 'primeng/selectbutton';
import { SizeService } from '../../../_services/size.service';
import { ColorService } from '../../../_services/color.service';
import { ColorPickerModule } from 'primeng/colorpicker';
import { InputSwitchModule } from 'primeng/inputswitch';
import { TooltipModule } from 'primeng/tooltip';
import { TagModule } from 'primeng/tag';
import { ToastrService } from '../../../_services/toastr.service';
import { ProductDto, ProductStatus, ProductUpdate } from '../../../_models/product.module';
import { PaginatedResult, Pagination } from '../../../_models/pagination';
import { VariantDto, VariantStatus, VariantUpdate } from '../../../_models/variant.module';
import { VariantService } from '../../../_services/variant.service';
@Component({
  selector: 'app-product',
  standalone: true,
  imports: [
    ButtonModule,
    CheckboxModule,
    CommonModule,
    TableModule,
    DropdownModule,
    InputTextModule,
    DialogModule,
    ToastModule,
    ReactiveFormsModule,
    PaginatorModule,
    ConfirmDialogModule,
    SelectButtonModule,
    ColorPickerModule,
    RouterModule,
    InputSwitchModule,
    TooltipModule,
    TagModule,
  ],
  providers: [ConfirmationService],
  templateUrl: './product.component.html',
  styleUrls: ['./product.component.css'],
})
export class ProductComponent {
  selectedProducts!: ProductDto[];
  selectedProduct: any;
  productForm!: FormGroup;
  categoryNames: { [key: number]: string } = {};
  sizeNames: { [key: number]: string } = {};
  colorCodes: { [key: number]: string } = {};
  colorCode: string = "";
  expandedRows: { [key: number]: boolean } = {};

  first: number = 0;
  pagination: Pagination = { currentPage: 1, itemPerPage: 10, totalItems: 0, totalPages: 1 };
  totalRecords: number = 0;
  pageSize: number = 5;
  pageNumber: number = 1;
  searchString: string = "";
  visible: boolean = false;

  productStatus: { name: string; key: ProductStatus }[] = [
    { name: 'Draft', key: ProductStatus.Draft },
    { name: 'Public', key: ProductStatus.Public },
  ];

  pageSizeOptions = [
    { label: '5', value: 5 },
    { label: '10', value: 10 },
    { label: '20', value: 20 },
    { label: '50', value: 50 },
  ];

  private subscriptions: Subscription = new Subscription();

  constructor(
    private builder: FormBuilder,
    private categoryService: CategoryService,
    private productService: ProductService,

    private toastService: ToastrService,
    private confirmationService: ConfirmationService,
    private router: Router,
    private messageService: MessageService
  ) {}

  ngOnInit() {
    this.initializeForm();
    this.loadProducts();
    this.fetchCategoryNames();
  }
  showDialog(product: any) {
    this.visible = true;
    this.selectedProduct = product;
  }

  initializeForm(): FormGroup {
    return this.builder.group({
      id: [0],
      name: ['', Validators.required],
      description: ['', Validators.required],
      status: [false],
    });
  }
  fetchCategoryNames() {
    this.categoryService.getAllCategories().subscribe((data: any[]) => {
      this.categoryNames = data.reduce((acc, category) => {
        acc[category.id] = category.name;
        return acc;
      }, {});
    });
  }

  getCategoryNameById(categoryId: number): string {
    return this.categoryNames[categoryId];
  }

  getStatus(status: number): boolean {
    if (status == 1) {
      return true;
    }
    return false;
  }
  setStatus(product: ProductDto, value: boolean) {
    const productUpdate: ProductUpdate = {
      id: product.id,
      name: product.name,
      description: product.description,
      categoryIds: product.categoryIds,
      status: value ? ProductStatus.Public : ProductStatus.Draft,
    };

    const formData = new FormData();
    formData.append('id', productUpdate.id.toString());
    formData.append('name', productUpdate.name);
    formData.append('description', productUpdate.description);
    productUpdate.categoryIds.forEach((categoryId: number) => {
      formData.append('categoryIds', categoryId.toString());
    });
    formData.append('status', productUpdate.status.toString());

    console.log(productUpdate);
    this.productService.updateProduct(formData).subscribe({
      next: () => {
        this.toastService.success('Cập nhật trạng thái thành công');
        this.loadProducts();
      },
      error: (err) => {
        console.error(err);
        this.toastService.error('Có lỗi xảy ra khi cập nhật trạng thái.');
      }
    });
  }

  truncateDescription(description: string, maxLength: number = 30): string {
    if (description.length > maxLength) {
      return description.substring(0, maxLength) + '...'; // Cắt và thêm dấu ba chấm
    }
    return description;
  }
  expandAll() {
    this.expandedRows = this.selectedProducts.reduce(
      (acc: { [key: number]: boolean }, p: ProductDto) => {
        acc[p.id] = true;
        return acc;
      },
      {}
    );
  }
  collapseAll() {
    this.expandedRows = {};
  }
  ngOnDestroy(): void {
    this.subscriptions.unsubscribe();
  }

  onSearch(): void {
    this.pageNumber = 1;
    this.loadProducts();
  }
  onPageChange(event: any): void {
    this.first = event.first;
    this.pageNumber = event.page + 1;
    this.pageSize = event.rows;
    this.loadProducts();
  }

  onPageSizeChange(event: Event): void {
    const target = event.target as HTMLSelectElement;
    if (target) {
      this.pageSize = +target.value;
      this.pageNumber = 1;
      this.loadProducts();
    }
  }
  loadProducts(): void {
    const params = {
      pageNumber: this.pageNumber,
      pageSize: this.pageSize,
      search: this.searchString
    };
    this.productService.getProductsPagedList(params, true).subscribe((result) => {
      this.selectedProducts = result.items || [];
      this.pagination = result.pagination ?? { currentPage: 1, itemPerPage: 10, totalItems: 0, totalPages: 1 };

      this.totalRecords = this.pagination.totalItems;
      this.first = (this.pageNumber - 1) * this.pageSize;
    });
  }
  getSeverity(status: string): 'success' | 'secondary' | 'info' | 'warning' | 'danger' | 'contrast' | undefined {
    switch (status) {
      case 'INSTOCK':
        return 'success';
      case 'LOWSTOCK':
        return 'warning';
      case 'OUTOFSTOCK':
        return 'danger';
      default:
        return 'secondary';
    }
  }
  getStatusSeverity(status: string): 'success' | 'secondary' | 'info' | 'warning' | 'danger' | 'contrast' | undefined {
    switch (status) {
      case 'success':
        return 'success';
      case 'warning':
        return 'warning';
      case 'danger':
        return 'danger';
      case 'unknown':
        return undefined;
      default:
        return 'secondary';
    }
  }
  onRowExpand(event: TableRowExpandEvent) {
    this.messageService.add({
      severity: 'info',
      summary: 'Product Expanded',
      detail: event.data.name,
      life: 3000,
    });
  }
  onRowCollapse(event: TableRowCollapseEvent) {
    this.messageService.add({
      severity: 'success',
      summary: 'Product Collapsed',
      detail: event.data.name,
      life: 3000,
    });
  }
  addProduct() {
    this.router.navigate(['/admin/product/add']);
  }
  updateProduct(id: number) {
    this.productService.getProductById(id).subscribe(() => {
      this.router.navigateByUrl('/admin/product/edit/' + id);
    });
  }

  confirmDelete(product: ProductDto): void {
    console.log("hiiiiiiii");
    this.confirmationService.confirm({
      message: 'Bạn có chắc chắn muốn xóa sản phẩm này?',
      accept: () => {
        const subscription = this.productService.deleteProduct(product.id).subscribe({
          next: () => {
            this.toastService.success('Sản phẩm đã được xóa thành công.');
            this.visible = false;
            this.loadProducts();
          },
          error: (err) => {
            this.toastService.error('Có lỗi xảy ra khi xóa sản phẩm.');
            console.error(err);
          }
        });

        this.subscriptions.add(subscription);
      }
    });
  }
}
