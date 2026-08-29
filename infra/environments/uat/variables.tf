variable "neon_api_key" {
  description = "Neon API key"
  type        = string
  sensitive   = true
}

variable "render_api_key" {
  description = "Render API key"
  type        = string
  sensitive   = true
}

variable "vercel_token" {
  description = "Vercel deployment token"
  type        = string
  sensitive   = true
}

variable "project_name" {
  description = "Project name for resource naming"
  type        = string
  default     = "fms"
}

variable "environment" {
  description = "Environment name"
  type        = string
  default     = "uat"
}

variable "region" {
  description = "AWS region for Neon"
  type        = string
  default     = "us-east-1"
}
