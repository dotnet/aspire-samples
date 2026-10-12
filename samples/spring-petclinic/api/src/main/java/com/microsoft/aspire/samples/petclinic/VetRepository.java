package com.microsoft.aspire.samples.petclinic;

import java.util.List;

import org.springframework.data.jpa.repository.JpaRepository;

public interface VetRepository extends JpaRepository<Vet, Long> {
    List<Vet> findAllByOrderByLastNameAscFirstNameAsc();
}
