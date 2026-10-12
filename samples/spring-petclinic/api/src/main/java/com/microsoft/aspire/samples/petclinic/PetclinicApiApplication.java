package com.microsoft.aspire.samples.petclinic;

import io.swagger.v3.oas.annotations.OpenAPIDefinition;
import io.swagger.v3.oas.annotations.info.Info;

import org.springframework.boot.SpringApplication;
import org.springframework.boot.autoconfigure.SpringBootApplication;

@SpringBootApplication
@OpenAPIDefinition(info = @Info(title = "Spring Petclinic API", version = "v1"))
public class PetclinicApiApplication {

	public static void main(String[] args) {
		SpringApplication.run(PetclinicApiApplication.class, args);
	}

}
