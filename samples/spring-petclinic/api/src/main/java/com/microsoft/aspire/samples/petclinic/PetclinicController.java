package com.microsoft.aspire.samples.petclinic;

import java.time.LocalDate;
import java.util.List;

import jakarta.validation.Valid;
import jakarta.validation.constraints.NotBlank;

import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.http.HttpStatus;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.RequestBody;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.ResponseStatus;
import org.springframework.web.bind.annotation.RestController;

// Trusted local demo: these unauthenticated routes expose all owner records and allow owner creation.
// Use synthetic data only, and do not expose or forward this API to untrusted networks.
@RestController
@RequestMapping("/api")
public class PetclinicController {
    private static final Logger logger = LoggerFactory.getLogger(PetclinicController.class);

    private final OwnerRepository owners;
    private final PetRepository pets;
    private final VetRepository vets;

    public PetclinicController(OwnerRepository owners, PetRepository pets, VetRepository vets) {
        this.owners = owners;
        this.pets = pets;
        this.vets = vets;
    }

    @GetMapping("/stats")
    public StatsResponse stats() {
        return new StatsResponse(owners.count(), pets.count(), vets.count());
    }

    @GetMapping("/owners")
    public List<OwnerResponse> owners() {
        return owners.findAllByOrderByLastNameAscFirstNameAsc().stream()
            .map(owner -> new OwnerResponse(
                owner.getId(),
                owner.getFirstName(),
                owner.getLastName(),
                owner.getAddress(),
                owner.getCity(),
                owner.getTelephone(),
                owner.getPets().stream()
                    .map(pet -> new PetResponse(pet.getId(), pet.getName(), pet.getType(), pet.getBirthDate()))
                    .toList()))
            .toList();
    }

    @PostMapping("/owners")
    @ResponseStatus(HttpStatus.CREATED)
    public OwnerResponse createOwner(@Valid @RequestBody CreateOwnerRequest request) {
        var owner = owners.save(new Owner(
            request.firstName(),
            request.lastName(),
            request.address(),
            request.city(),
            request.telephone()));

        logger.info("Created owner {}", owner.getId());

        return new OwnerResponse(
            owner.getId(),
            owner.getFirstName(),
            owner.getLastName(),
            owner.getAddress(),
            owner.getCity(),
            owner.getTelephone(),
            List.of());
    }

    @GetMapping("/vets")
    public List<VetResponse> vets() {
        return vets.findAllByOrderByLastNameAscFirstNameAsc().stream()
            .map(vet -> new VetResponse(vet.getId(), vet.getFirstName(), vet.getLastName(), vet.getSpecialty()))
            .toList();
    }

    public record StatsResponse(long owners, long pets, long vets) {
    }

    public record OwnerResponse(
        Long id,
        String firstName,
        String lastName,
        String address,
        String city,
        String telephone,
        List<PetResponse> pets) {
    }

    public record PetResponse(Long id, String name, String type, LocalDate birthDate) {
    }

    public record VetResponse(Long id, String firstName, String lastName, String specialty) {
    }

    public record CreateOwnerRequest(
        @NotBlank String firstName,
        @NotBlank String lastName,
        @NotBlank String address,
        @NotBlank String city,
        @NotBlank String telephone) {
    }
}
