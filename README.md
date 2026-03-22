# ShopNongSan – E-commerce Website

ShopNongSan là **website thương mại điện tử bán nông sản** được xây dựng bằng **ASP.NET Core 8 MVC**.
Hệ thống cho phép người dùng xem sản phẩm, thêm vào giỏ hàng, đặt hàng và thanh toán trực tuyến.

🌐 Live Website
https://shopnongsan.id.vn

📂 Source Code
https://github.com/sugartank555/ShopNongSan

---

# Author

**Bùi Hải Đường**
Backend .NET Developer

Email: [duong17624@gmail.com](mailto:duong17624@gmail.com)
GitHub: https://github.com/sugartank555

---

# Project Overview

ShopNongSan là hệ thống **E-commerce Web Application** cho phép:

* Xem danh sách sản phẩm
* Xem chi tiết sản phẩm
* Thêm sản phẩm vào giỏ hàng
* Đặt hàng trực tuyến
* Thanh toán online
* Quản lý đơn hàng

Hệ thống được xây dựng theo **MVC architecture** và sử dụng **Entity Framework Core** để làm việc với database.

---

# Tech Stack

Backend

* ASP.NET Core 8 MVC
* C#
* Entity Framework Core
* SQL Server

Frontend

* HTML
* CSS
* Bootstrap
* JavaScript

Infrastructure

* Docker
* GitHub

Payment Integration

* PayOS API

---

# Main Features

## Product

* Hiển thị danh sách sản phẩm
* Xem chi tiết sản phẩm
* Phân loại theo danh mục

## Shopping Cart

* Thêm sản phẩm vào giỏ hàng
* Cập nhật số lượng
* Xóa sản phẩm khỏi giỏ hàng

## Order

* Tạo đơn hàng
* Lưu thông tin khách hàng
* Quản lý trạng thái đơn hàng

## Payment

* Tích hợp thanh toán online với PayOS
* Xử lý callback thanh toán

---

# Demo Login

## Admin Account

Email: [admin@shopnongsan.local](mailto:admin@shopnongsan.local)
Password: Admin@123

Admin có quyền:

* Quản lý sản phẩm
* Quản lý danh mục
* Quản lý đơn hàng
* Quản lý khách hàng

---

# System Architecture

Client Browser
↓
ASP.NET Core MVC
↓
Controllers
↓
Business Logic
↓
Entity Framework Core
↓
SQL Server Database

External Service

PayOS Payment Gateway

---

# Database

Các bảng chính:

* Product
* Category
* Order
* OrderItem
* Customer

Quan hệ:

Category → nhiều Product

Order → nhiều OrderItem

OrderItem → liên kết Product

---

# Run Project Locally

Clone repository

git clone https://github.com/sugartank555/ShopNongSan

Di chuyển vào thư mục project

cd ShopNongSan

Restore packages

dotnet restore

Run project

dotnet run

Mở trình duyệt

https://localhost:7181

---

# Docker

Build image

docker build -t shopnongsan .

Run container

docker run -p 8080:80 shopnongsan

---

# Deployment

Production deployment:

User
↓
Domain: shopnongsan.id.vn
↓
Cloud Server
↓
Docker Container
↓
ASP.NET Core Application
↓
SQL Server Database

---

# Purpose

Project được xây dựng nhằm:

* Thực hành phát triển web với ASP.NET Core
* Xây dựng hệ thống thương mại điện tử
* Triển khai ứng dụng bằng Docker
* Xây dựng portfolio Backend .NET Developer
