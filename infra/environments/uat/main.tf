terraform {
  required_providers {
    neon = {
      source  = "kislerdm/neon"
      version = "~> 0.2"
    }
  }
  required_version = ">= 1.0"

  backend "local" {}
}

provider "neon" {
  api_key = var.neon_api_key
}

# ==========================================
# Neon PostgreSQL Database
# ==========================================
resource "neon_project" "fms_uat" {
  name = "${var.project_name}-${var.environment}"

  default_branch_name = "main"

  history_retention = 7
}

resource "neon_branch" "uat" {
  project_id = neon_project.fms_uat.id
  name       = "uat"
  parent_id = neon_project.fms_uat.default_branch_id
}

resource "neon_database" "rgbsi_fleet" {
  project_id = neon_project.fms_uat.id
  branch_id  = neon_branch.uat.id
  name       = "rgbsi_fleet"
}

resource "neon_role" "app_user" {
  project_id = neon_project.fms_uat.id
  branch_id  = neon_branch.uat.id
  name       = "fms_app"
}

# ==========================================
# Outputs
# ==========================================
output "neon_project_id" {
  value = neon_project.fms_uat.id
}

output "neon_database_url" {
  value     = "postgres://${neon_role.app_user.name}:${neon_role.app_user.password}@${neon_project.fms_uat.database_host}/${neon_database.rgbsi_fleet.name}"
  sensitive = true
}

output "neon_branch_id" {
  value = neon_branch.uat.id
}
