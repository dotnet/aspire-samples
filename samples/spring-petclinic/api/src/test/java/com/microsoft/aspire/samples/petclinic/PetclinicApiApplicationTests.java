package com.microsoft.aspire.samples.petclinic;

import static org.hamcrest.Matchers.containsString;
import static org.hamcrest.Matchers.hasItem;
import static org.springframework.test.web.servlet.request.MockMvcRequestBuilders.get;
import static org.springframework.test.web.servlet.request.MockMvcRequestBuilders.post;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.content;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.jsonPath;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.status;

import org.junit.jupiter.api.Test;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.boot.test.context.SpringBootTest;
import org.springframework.boot.webmvc.test.autoconfigure.AutoConfigureMockMvc;
import org.springframework.http.MediaType;
import org.springframework.test.context.ActiveProfiles;
import org.springframework.test.web.servlet.MockMvc;
import org.springframework.transaction.annotation.Transactional;

@SpringBootTest
@AutoConfigureMockMvc
@ActiveProfiles("test")
@Transactional
class PetclinicApiApplicationTests {

	@Autowired
	private MockMvc mvc;

	@Test
	void scalarServesTheGeneratedApiReference() throws Exception {
		mvc.perform(get("/scalar"))
			.andExpect(status().isOk())
			.andExpect(content().contentTypeCompatibleWith(MediaType.TEXT_HTML))
			.andExpect(content().string(containsString("Spring Petclinic API")))
			.andExpect(content().string(containsString("/v3/api-docs")))
			.andExpect(content().string(containsString("/scalar/scalar.js")));

		mvc.perform(get("/scalar/scalar.js"))
			.andExpect(status().isOk());
	}

	@Test
	void openApiDescribesTheExistingOperationsAndValidation() throws Exception {
		mvc.perform(get("/v3/api-docs"))
			.andExpect(status().isOk())
			.andExpect(jsonPath("$.info.title").value("Spring Petclinic API"))
			.andExpect(jsonPath("$.paths['/api/stats'].get").exists())
			.andExpect(jsonPath("$.paths['/api/owners'].get").exists())
			.andExpect(jsonPath("$.paths['/api/owners'].post.responses['201']").exists())
			.andExpect(jsonPath("$.paths['/api/vets'].get").exists())
			.andExpect(jsonPath("$.paths['/actuator/health']").doesNotExist())
			.andExpect(jsonPath("$.components.schemas.CreateOwnerRequest.required", hasItem("firstName")));
	}

	@Test
	void existingPetclinicDataIsAvailable() throws Exception {
		mvc.perform(get("/api/stats"))
			.andExpect(status().isOk())
			.andExpect(jsonPath("$.owners").value(3))
			.andExpect(jsonPath("$.pets").value(4))
			.andExpect(jsonPath("$.vets").value(3));

		mvc.perform(get("/api/owners"))
			.andExpect(status().isOk())
			.andExpect(jsonPath("$[*].pets[*].name", hasItem("Leo")));

		mvc.perform(get("/api/vets"))
			.andExpect(status().isOk())
			.andExpect(jsonPath("$[*].specialty", hasItem("Radiology")));
	}

	@Test
	void creatingAnOwnerPreservesTheExistingContract() throws Exception {
		mvc.perform(post("/api/owners")
				.contentType(MediaType.APPLICATION_JSON)
				.content("""
					{"firstName":"Alex","lastName":"Rivera","address":"123 Sample St.","city":"Madison","telephone":"6085550100"}
					"""))
			.andExpect(status().isCreated())
			.andExpect(jsonPath("$.id").isNumber())
			.andExpect(jsonPath("$.firstName").value("Alex"))
			.andExpect(jsonPath("$.pets").isEmpty());

		mvc.perform(get("/api/owners"))
			.andExpect(status().isOk())
			.andExpect(jsonPath("$[*].lastName", hasItem("Rivera")));
	}

	@Test
	void invalidOwnersAreRejected() throws Exception {
		mvc.perform(post("/api/owners")
				.contentType(MediaType.APPLICATION_JSON)
				.content("{}"))
			.andExpect(status().isBadRequest());
	}
}
