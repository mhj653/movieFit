ProcessVideoAnalyzer release package

실행 방법
1. ProcessVideoAnalyzer-win-x64.tar 파일을 대상 PC로 복사합니다.
2. 아래 명령으로 압축을 풉니다.
   tar -xf ProcessVideoAnalyzer-win-x64.tar
3. 풀린 폴더의 ProcessVideoAnalyzer.exe를 실행합니다.

GitHub Release에 큰 파일 업로드가 어려운 경우
1. 아래 3개 분할 파일을 모두 같은 폴더에 둡니다.
   ProcessVideoAnalyzer-win-x64.tar.part01
   ProcessVideoAnalyzer-win-x64.tar.part02
   ProcessVideoAnalyzer-win-x64.tar.part03
2. 아래 명령으로 원본 tar를 복원합니다.
   copy /b ProcessVideoAnalyzer-win-x64.tar.part01+ProcessVideoAnalyzer-win-x64.tar.part02+ProcessVideoAnalyzer-win-x64.tar.part03 ProcessVideoAnalyzer-win-x64.tar
3. 복원된 tar 파일을 압축 해제한 뒤 ProcessVideoAnalyzer.exe를 실행합니다.

소스 패키지
- movieFit-source-refactored.tar.gz

빌드 기준
- .NET 8
- Windows x64
