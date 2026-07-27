set(SHARPDETECT_TEST_RESULTS_DIR "${CMAKE_BINARY_DIR}/TestResults" CACHE PATH
	"Directory receiving native JUnit test reports")
file(MAKE_DIRECTORY "${SHARPDETECT_TEST_RESULTS_DIR}")

function(add_doctest_test target_name)
	add_test(NAME ${target_name} COMMAND ${target_name}
		--reporters=junit
		--out=${SHARPDETECT_TEST_RESULTS_DIR}/native-tests-${target_name}.xml)
endfunction()
