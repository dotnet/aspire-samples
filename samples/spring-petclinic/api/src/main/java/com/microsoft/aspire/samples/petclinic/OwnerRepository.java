package com.microsoft.aspire.samples.petclinic;

import java.util.List;

import org.springframework.data.jpa.repository.EntityGraph;
import org.springframework.data.jpa.repository.JpaRepository;

public interface OwnerRepository extends JpaRepository<Owner, Long> {
    @EntityGraph(attributePaths = "pets")
    List<Owner> findAllByOrderByLastNameAscFirstNameAsc();
}
