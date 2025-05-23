import {
  Component,
  EventEmitter,
  Input,
  input,
  Output,
  output,
} from '@angular/core';
import { TreeModule } from 'primeng/tree';
import { CommonModule } from '@angular/common';
import { RouterLink, RouterModule } from '@angular/router';
import { SidebarModule as PrimeNgSidebarModule } from 'primeng/sidebar';
import { TreeNode } from 'primeng/api';
@Component({
  selector: 'app-sidebar',
  standalone: true,
  imports: [CommonModule, RouterModule, PrimeNgSidebarModule, TreeModule],
  templateUrl: './sidebar.component.html',
  styleUrls: ['./sidebar.component.css'],
})
export class SidebarComponent {
  @Input() isSidebarCollapsed: boolean = false;
  @Output() changeIsSidebarCollapsed = new EventEmitter<boolean>();
  items = [
    {
      routerLink: 'statistic',
      icon: 'pi pi-chart-line',
      label: 'Thống kê',
    },
    {
      routerLink: 'category',
      icon: 'pi pi-book',
      label: 'Danh mục',
    },
    {
      routerLink: 'product',
      icon: 'pi pi-gift',
      label: 'Sản phẩm',
    },
    {
      routerLink: 'chat',
      icon: 'pi pi-comments',
      label: 'Chat',
    },
    {
      routerLink: 'user',
      icon: 'pi pi-users',
      label: 'Quản lý người dùng',
    },
    {
      routerLink: 'role',
      icon: 'pi pi-key',
      label: 'Quyền',
    },
    {
      routerLink: 'order',
      icon: 'pi pi-check-square',
      label: 'Quản lý đơn hàng',
    },

    {
      routerLink: 'blog',
      icon: 'pi pi-receipt',
      label: 'Quản lý bài viết',
    },

    {
      routerLink: 'shippingmethod',
      icon: 'pi pi-truck',
      label: 'Quản lý phương thức Ship',
    },
    {
      routerLink: 'discount',
      icon: 'pi pi-tag',
      label: 'Quản lý Mã giảm giá',
    },
  ];

  toggleCollapse(): void {
    this.changeIsSidebarCollapsed.emit(!this.isSidebarCollapsed);
  }

  closeSidenav(): void {
    this.changeIsSidebarCollapsed.emit(true);
  }
}
